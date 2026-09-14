using KiwiCart.Core.DTOs;
using KiwiCart.Core.Entities;
using KiwiCart.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KiwiCart.Infrastructure.Services;

public class PriceComparisonService : IPriceComparisonService
{
    private readonly IPriceCacheRepository _cache;
    private readonly IStoreAggregator _aggregator;
    private readonly IPriceCalculator _calculator;
    private readonly IProductGtinRepository _gtins;
    private readonly IStoreService _stores;
    private readonly ILogger<PriceComparisonService> _logger;

    // The store selected for each dynamic-store brand this request by location.
    // Scoped per request, so instance state is safe here.
    private readonly Dictionary<string, Store> _selectedStores = new(StringComparer.OrdinalIgnoreCase);

    // Brands to exclude from cached results when a caller explicitly marks a
    // brand unavailable. Live price lookup falls back to the client's default
    // store when location data has not yet been synchronized.
    private readonly HashSet<string> _droppedBrands = new(StringComparer.OrdinalIgnoreCase);

    // Brands that select their store dynamically by location AND query that
    // store's prices (Foodstuffs: the search API accepts a storeId). These are
    // queried using the selected store when one is available.
    private static readonly string[] DynamicStoreBrands = ["PakNSave", "NewWorld"];

    // Brands whose prices are national (no per-store pricing / storeId in the
    // search API), but for which we still show the nearest physical store on the
    // map. These are NOT dropped when none is within range — the price is still
    // valid nationwide; only the displayed store falls back to the default.
    private static readonly string[] DisplayOnlyStoreBrands = ["Woolworths"];

    public PriceComparisonService(
        IPriceCacheRepository cache,
        IStoreAggregator aggregator,
        IPriceCalculator calculator,
        IProductGtinRepository gtins,
        IStoreService stores,
        ILogger<PriceComparisonService> logger)
    {
        _cache = cache;
        _aggregator = aggregator;
        _calculator = calculator;
        _gtins = gtins;
        _stores = stores;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PriceResult>> CompareAsync(
        string searchTerm, CancellationToken ct = default,
        double? lat = null, double? lng = null)
    {
        _logger.LogInformation("CompareAsync: term={Term}, lat={Lat}, lng={Lng}", searchTerm, lat, lng);
        
        _selectedStores.Clear();
        _droppedBrands.Clear();
        
        // Resolve the nearest priceable store per dynamic-store brand (Pak'nSave)
        // from the user's location, so live fetches query that store's prices.
        var storeIdsByBrand = await ResolveStoreSelectionAsync(lat, lng, ct);
        _logger.LogInformation("ResolveStoreSelection returned {Count} brands", storeIdsByBrand?.Count ?? 0);

        // Read whatever is currently cached for this term.
        var cached = await _cache.GetCachedPricesAsync(searchTerm, ct);

        // When location was supplied but a brand has no store within range,
        // drop any cached rows for that brand so it is absent from both the fast
        // path and the missing-brand computation below.
        if (_droppedBrands.Count > 0)
        {
            cached = cached
                .Where(r => !_droppedBrands.Contains(r.StoreBrand ?? ""))
                .ToList();
        }

        // Fill in missing DisplayProductNames for cached data
        foreach (var r in cached)
        {
            if (string.IsNullOrEmpty(r.DisplayProductName))
            {
                r.DisplayProductName = r.ProductName; // For cached data, use original name
            }
        }

        // Determine which stores are already represented in the cache. If every
        // known store has at least one cached row for this term, the cache is
        // considered complete and we serve it directly (fast path).
        var cachedBrands = cached
            .Select(r => r.StoreBrand)
            .Where(b => !string.IsNullOrEmpty(b))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // The set of brands we expect results from. Brands dropped for this
        // request (location given, none within range) are excluded so the fast
        // path and fetch don't wait for them.
        var expectedBrands = _droppedBrands.Count > 0
            ? _aggregator.KnownStoreBrands
                .Where(b => !_droppedBrands.Contains(b))
                .ToList()
            : _aggregator.KnownStoreBrands.ToList();

        var missingBrands = expectedBrands
            .Where(b => !cachedBrands.Contains(b))
            .ToList();

        if (cached.Count > 0 && missingBrands.Count == 0)
        {
            // Cache hit: build storeMapping with selected stores and apply
            var cachStoreMapping = new Dictionary<string, (string name, string address, double lat, double lng)>
            {
                { "PakNSave", ("PAK'nSAVE Mt Albert", "Mt Albert", -36.89305, 174.70624) },
                { "NewWorld", ("New World Mt Roskill", "Mt Roskill", -36.908622, 174.734362) },
                { "Woolworths", ("Mount Roskill Woolworths", "Mt Roskill", -36.9042, 174.727) }
            };
            
            foreach (var (brand, store) in _selectedStores)
            {
                cachStoreMapping[brand] = (store.Name, store.Address, store.Latitude, store.Longitude);
            }
            
            var cacheResult = cached.OrderBy(r => r.Price).ToList();
            foreach (var r in cacheResult)
            {
                if (cachStoreMapping.TryGetValue(r.StoreBrand, out var storeInfo))
                {
                    r.StoreName = storeInfo.name;
                    r.Address = storeInfo.address;
                    r.Lat = storeInfo.lat;
                    r.Lng = storeInfo.lng;
                }
            }
            
            await EnrichWithGtinsAsync(cacheResult, ct);
            return cacheResult;
        }

        // Otherwise, live-fetch the stores missing from the cache (or every
        // store on a full cache miss) and merge with the cached rows. This
        // prevents one store's cached rows from masking the others, which
        // previously made products look store-exclusive (e.g. a product cached
        // only for Pak'nSave appeared to be sold only at Pak'nSave).
        var brandsToFetch = cached.Count == 0 ? expectedBrands : missingBrands;

        _logger.LogInformation(
            "Cache incomplete for '{Term}' (have: [{Have}], fetching: [{Fetch}])",
            searchTerm, string.Join(", ", cachedBrands), string.Join(", ", brandsToFetch));

        var live = await _aggregator.SearchStoresAsync(brandsToFetch, searchTerm, ct, storeIdsByBrand);

        // Merge: cached rows for stores already present + freshly fetched rows.
        var merged = new List<PriceResult>(cached);
        merged.AddRange(live);

        // Map store brand to correct store details (fix hardcoded values from clients)
        var storeMapping = new Dictionary<string, (string name, string address, double lat, double lng)>
        {
            { "PakNSave", ("PAK'nSAVE Mt Albert", "Mt Albert", -36.89305, 174.70624) },
            { "NewWorld", ("New World Mt Roskill", "Mt Roskill", -36.908622, 174.734362) },
            { "Woolworths", ("Mount Roskill Woolworths", "Mt Roskill", -36.9042, 174.727) }
        };

        // For any brand whose store was selected by location, reflect its real
        // name/address/coords on the results instead of the hardcoded default.
        foreach (var (brand, store) in _selectedStores)
        {
            storeMapping[brand] =
                (store.Name, store.Address, store.Latitude, store.Longitude);
            _logger.LogInformation("storeMapping updated: {Brand} -> {Name}", brand, store.Name);
        }

        foreach (var r in live)
        {
            if (storeMapping.TryGetValue(r.StoreBrand, out var storeInfo))
            {
                _logger.LogInformation("Applying storeMapping to {Brand}: {OldName} -> {NewName}", 
                    r.StoreBrand, r.StoreName, storeInfo.name);
                r.StoreName = storeInfo.name;
                r.Address = storeInfo.address;
                r.Lat = storeInfo.lat;
                r.Lng = storeInfo.lng;
            }
        }

        foreach (var r in merged)
        {
            if (string.IsNullOrEmpty(r.DisplayProductName))
            {
                r.DisplayProductName = r.ProductName;
            }
        }

        // Background cache upsert for the freshly fetched rows (non-blocking).
        if (live.Count > 0)
        {
            _ = Task.Run(async () =>
            {
                foreach (var r in live)
                {
                    try { await _cache.UpsertPriceAsync(r, CancellationToken.None); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Failed to cache price for {Product}", r.ProductName); }

                    // Opportunistically record the product in product_gtins. Any
                    // row with a platform product id is recorded: Woolworths
                    // arrives with a GTIN (from barcode); Foodstuffs arrives
                    // without one (gtin stays null) and is backfilled later via
                    // the admin detail-endpoint API. Existing rows and any GTIN
                    // already stored are preserved.
                    if (!string.IsNullOrEmpty(r.ProductId))
                    {
                        try { await _gtins.UpsertForBackfillAsync(r, CancellationToken.None); }
                        catch (Exception ex) { _logger.LogWarning(ex, "Failed to record GTIN row for {Product}", r.ProductName); }
                    }
                }
            }, CancellationToken.None);
        }

        // Enrich results with GTINs (batch lookup) so the frontend can merge the
        // same product across platforms. Rows keep a null gtin if unknown.
        await EnrichWithGtinsAsync(merged, ct);

        return merged.OrderBy(r => r.Price).ToList();
    }

    /// <summary>
    /// From the user's location, pick the nearest priceable store for each
    /// dynamic-store brand (Pak'nSave, New World) and return a brand→storeId map
    /// for the live fetch. For display-only brands (Woolworths) it resolves the
    /// nearest physical store for the map but neither returns a storeId nor drops
    /// the brand. When a location is supplied but a dynamic brand has no store
    /// within the nearby radius, that brand is dropped. Returns null when no
    /// location is supplied or no dynamic brand resolved a store.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, string>?> ResolveStoreSelectionAsync(
        double? lat, double? lng, CancellationToken ct)
    {
        if (lat is null || lng is null)
            return null;

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var brand in DynamicStoreBrands)
        {
            var store = await _stores.GetNearestStoreWithExternalIdAsync(
                brand, lat.Value, lng.Value, StoreService.NearbyRadiusKm, ct);

            if (store is null)
            {
                // No store within range at all, drop the brand
                _droppedBrands.Add(brand);
                _logger.LogInformation(
                    "No {Brand} store within {Radius}km of {Lat},{Lng}; dropping {Brand}",
                    brand, StoreService.NearbyRadiusKm, lat, lng, brand);
                continue;
            }

            _selectedStores[brand] = store;
            
            // Only add to map if it has external_store_id (can be queried for prices)
            if (store.ExternalStoreId is not null)
            {
                map[brand] = store.ExternalStoreId;
                _logger.LogInformation("Selected {Brand} store {Name} ({Id}) for location {Lat},{Lng}",
                    brand, store.Name, store.ExternalStoreId, lat, lng);
            }
            else
            {
                _logger.LogInformation(
                    "Selected {Brand} store {Name} for location {Lat},{Lng} (no external ID; using default prices)",
                    brand, store.Name, lat, lng);
            }
        }

        // Display-only brands: resolve the nearest physical store for the map
        // only. Prices are national, so no storeId is passed and the brand is
        // never dropped. If none is within range, keep the default display store.
        foreach (var brand in DisplayOnlyStoreBrands)
        {
            var store = await _stores.GetNearestStoreWithExternalIdAsync(
                brand, lat.Value, lng.Value, double.MaxValue, ct);
            if (store is not null)
            {
                _selectedStores[brand] = store;
                _logger.LogInformation(
                    "Nearest {Brand} store for map: {Name} ({Id})",
                    brand, store.Name, store.ExternalStoreId);
            }
        }

        return map.Count > 0 ? map : null;
    }

    /// <summary>
    /// Batch-resolve GTINs for the given rows and assign PriceResult.Gtin so
    /// cross-platform grouping works client-side. Best-effort: on any failure
    /// the rows are returned without GTINs rather than failing the search.
    /// </summary>
    private async Task EnrichWithGtinsAsync(IReadOnlyList<PriceResult> rows, CancellationToken ct)
    {
        try
        {
            var keys = rows
                .Where(r => !string.IsNullOrEmpty(r.StoreBrand) && !string.IsNullOrEmpty(r.ProductId))
                .Select(r => (r.StoreBrand, r.ProductId!))
                .Distinct()
                .ToList();

            if (keys.Count == 0)
                return;

            var gtins = await _gtins.GetGtinsForAsync(keys, ct);
            foreach (var r in rows)
            {
                if (string.IsNullOrEmpty(r.StoreBrand) || string.IsNullOrEmpty(r.ProductId))
                    continue;
                if (gtins.TryGetValue($"{r.StoreBrand}|{r.ProductId}", out var gtin))
                    r.Gtin = gtin;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GTIN enrichment failed; returning results without GTINs");
        }
    }
}

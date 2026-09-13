using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KiwiCart.Infrastructure.Services;

public class PriceComparisonService : IPriceComparisonService
{
    private readonly IPriceCacheRepository _cache;
    private readonly IStoreAggregator _aggregator;
    private readonly IPriceCalculator _calculator;
    private readonly ILogger<PriceComparisonService> _logger;

    public PriceComparisonService(
        IPriceCacheRepository cache,
        IStoreAggregator aggregator,
        IPriceCalculator calculator,
        ILogger<PriceComparisonService> logger)
    {
        _cache = cache;
        _aggregator = aggregator;
        _calculator = calculator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PriceResult>> CompareAsync(string searchTerm, CancellationToken ct = default)
    {
        // Read whatever is currently cached for this term.
        var cached = await _cache.GetCachedPricesAsync(searchTerm, ct);

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

        var missingBrands = _aggregator.KnownStoreBrands
            .Where(b => !cachedBrands.Contains(b))
            .ToList();

        if (cached.Count > 0 && missingBrands.Count == 0)
        {
            // Cache hit: return directly
            return cached.OrderBy(r => r.Price).ToList();
        }

        // Otherwise, live-fetch the stores missing from the cache (or every
        // store on a full cache miss) and merge with the cached rows. This
        // prevents one store's cached rows from masking the others, which
        // previously made products look store-exclusive (e.g. a product cached
        // only for Pak'nSave appeared to be sold only at Pak'nSave).
        var brandsToFetch = cached.Count == 0 ? _aggregator.KnownStoreBrands : missingBrands;

        _logger.LogInformation(
            "Cache incomplete for '{Term}' (have: [{Have}], fetching: [{Fetch}])",
            searchTerm, string.Join(", ", cachedBrands), string.Join(", ", brandsToFetch));

        var live = await _aggregator.SearchStoresAsync(brandsToFetch, searchTerm, ct);

        // Merge: cached rows for stores already present + freshly fetched rows.
        var merged = new List<PriceResult>(cached);
        merged.AddRange(live);

        // Map store brand to correct store details (fix hardcoded values from clients)
        var storeMapping = new Dictionary<string, (string name, string address, double lat, double lng)>
        {
            { "PakNSave", ("Pak'nSave Royal Oak", "Henderson, West Auckland", -36.8819, 174.6336) },
            { "NewWorld", ("New World Victoria Park", "Victoria Park, Auckland", -36.8485, 174.7523) },
            { "Woolworths", ("Woolworths Auckland City", "Grey Lynn, Auckland", -36.8645, 174.7431) }
        };

        // Update live results with correct store details
        foreach (var r in live)
        {
            if (storeMapping.TryGetValue(r.StoreBrand, out var storeInfo))
            {
                r.StoreName = storeInfo.name;
                r.Address = storeInfo.address;
                r.Lat = storeInfo.lat;
                r.Lng = storeInfo.lng;
            }
        }

        // Ensure all items have DisplayProductName (fallback to ProductName if not set)
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
                }
            }, CancellationToken.None);
        }

        return merged.OrderBy(r => r.Price).ToList();
    }
}

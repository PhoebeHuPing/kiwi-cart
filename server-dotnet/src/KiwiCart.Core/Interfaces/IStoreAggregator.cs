using KiwiCart.Core.DTOs;

namespace KiwiCart.Core.Interfaces;

public interface IStoreAggregator
{
    /// <summary>The store brand identifiers backed by a live client (e.g. PakNSave, NewWorld, Woolworths).</summary>
    IReadOnlyCollection<string> KnownStoreBrands { get; }

    Task<IReadOnlyList<PriceResult>> SearchAllStoresAsync(
        string term, CancellationToken ct = default,
        IReadOnlyDictionary<string, string>? storeIdsByBrand = null);

    /// <summary>Live-fetch only the given store brands (used to backfill stores missing from cache).</summary>
    Task<IReadOnlyList<PriceResult>> SearchStoresAsync(
        IReadOnlyCollection<string> storeBrands, string term, CancellationToken ct = default,
        IReadOnlyDictionary<string, string>? storeIdsByBrand = null);

    /// <summary>Search for a product by exact GTIN (barcode) across specified store brands.</summary>
    Task<IReadOnlyList<PriceResult>> SearchByGtinAsync(
        IReadOnlyCollection<string> storeBrands, string gtin, CancellationToken ct = default,
        IReadOnlyDictionary<string, string>? storeIdsByBrand = null);
}

using KiwiCart.Core.DTOs;

namespace KiwiCart.Core.Interfaces;

public interface IPriceCacheRepository
{
    Task<IReadOnlyList<PriceResult>> GetCachedPricesAsync(string searchTerm, CancellationToken ct = default);

    /// <summary>
    /// Get cached prices for a product across all supermarkets by GTIN. Joins
    /// product_gtins (GTIN -> per-brand external_product_id) to products and
    /// prices, constraining the store brand so Foodstuffs' shared product ids
    /// do not cross-match. Returns the most recent price per store brand within
    /// the cache window.
    /// </summary>
    Task<IReadOnlyList<PriceResult>> GetCachedPricesByGtinAsync(string gtin, CancellationToken ct = default);

    Task UpsertPriceAsync(PriceResult price, CancellationToken ct = default);
}

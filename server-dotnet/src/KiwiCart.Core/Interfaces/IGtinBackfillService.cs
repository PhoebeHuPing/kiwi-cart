namespace KiwiCart.Core.Interfaces;

/// <summary>
/// Admin-only backfill of the product_gtins table. These operations deep-fetch
/// from a store and record product ids + GTINs, and are intended to run on
/// demand (not on the user search path).
/// </summary>
public interface IGtinBackfillService
{
    /// <summary>
    /// Deep-fetch all Woolworths products matching a search term and upsert
    /// their GTIN rows. Returns the number of rows inserted or updated.
    /// </summary>
    Task<GtinBackfillResult> BackfillWoolworthsByNameAsync(string searchTerm, CancellationToken ct = default);

    /// <summary>
    /// Look up a single Woolworths product by its sku and upsert its GTIN row.
    /// Returns the number of rows inserted or updated (0 or 1).
    /// </summary>
    Task<GtinBackfillResult> BackfillWoolworthsBySkuAsync(string sku, CancellationToken ct = default);

    /// <summary>
    /// API 3: resolve missing GTINs for Foodstuffs rows. Selects up to
    /// <paramref name="count"/> rows that have a product id but no GTIN, calls
    /// each store's detail endpoint to fetch the GTIN, and backfills it. A delay
    /// is applied between calls to rate-limit the upstream requests.
    /// </summary>
    Task<GtinBackfillResult> BackfillMissingFoodstuffsGtinsAsync(
        int count, int delayMs, CancellationToken ct = default);
}

/// <summary>Summary of a backfill run.</summary>
public record GtinBackfillResult(int Fetched, int Inserted, int Updated, int Skipped);

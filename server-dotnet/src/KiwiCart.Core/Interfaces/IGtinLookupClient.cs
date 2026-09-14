namespace KiwiCart.Core.Interfaces;

/// <summary>
/// A store client that can resolve a single product's GTIN from its detail
/// endpoint, given the platform's product id. Used to backfill GTINs for
/// Foodstuffs rows (Pak'nSave / New World), whose search results omit the GTIN
/// but whose product detail endpoint exposes it as "sku".
/// </summary>
public interface IGtinLookupClient
{
    /// <summary>Stable store brand key, e.g. "PakNSave" or "NewWorld".</summary>
    string StoreBrand { get; }

    /// <summary>
    /// Fetch the raw GTIN for a product id via the store's detail endpoint.
    /// Returns null if not found or unavailable. The value is not normalized.
    /// </summary>
    Task<string?> FetchGtinByProductIdAsync(string productId, CancellationToken ct = default);
}

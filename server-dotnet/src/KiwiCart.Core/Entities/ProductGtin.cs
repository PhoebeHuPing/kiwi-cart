namespace KiwiCart.Core.Entities;

/// <summary>
/// Maps a single store's product to its GTIN (Global Trade Item Number), the
/// barcode shared across retailers for the same physical product. Rows with
/// the same normalized <see cref="Gtin"/> across different store brands refer
/// to the same product, which is how cross-platform price comparison is done.
///
/// GTIN sources per platform:
/// - Woolworths: the "barcode" field is present directly in search results.
/// - Foodstuffs (Pak'nSave / New World): the "sku" field is only on the
///   product detail endpoint (/v1/edge/store/{storeId}/product/{productId}).
/// </summary>
public class ProductGtin
{
    public int Id { get; set; }

    /// <summary>Stable store brand key: "PakNSave", "NewWorld", or "Woolworths".</summary>
    public string StoreBrand { get; set; } = string.Empty;

    /// <summary>
    /// The platform's own product identifier (Foodstuffs "5000527-EA-000",
    /// Woolworths sku "282768"). Unique per store brand.
    /// </summary>
    public string ExternalProductId { get; set; } = string.Empty;

    /// <summary>
    /// Normalized GTIN-14 (zero-padded from GTIN-8/12/13). This is the
    /// cross-platform matching key: same value across store brands = same product.
    /// Nullable: a row may be created from a platform's product id before its
    /// GTIN is known (e.g. Foodstuffs, whose GTIN needs a detail-endpoint call),
    /// and backfilled later.
    /// </summary>
    public string? Gtin { get; set; }

    /// <summary>Product name as captured at scrape time (for manual review).</summary>
    public string? ProductName { get; set; }

    /// <summary>Product brand, e.g. "Anchor", "Pams".</summary>
    public string? ProductBrand { get; set; }

    /// <summary>Product size/pack, e.g. "250ml", "1L", "200g".</summary>
    public string? ProductSize { get; set; }

    /// <summary>
    /// Flags a row for manual review/confirmation. Defaults to false. Can be
    /// set true for uncertain matches so a human can verify or correct them later.
    /// </summary>
    public bool NeedConfirm { get; set; }

    /// <summary>
    /// True once a GTIN lookup has been attempted for this row (used by the
    /// Foodstuffs backfill). Prevents re-fetching rows whose GTIN cannot be
    /// resolved — e.g. weighed produce (KGM) has no barcode. Set regardless of
    /// whether the lookup succeeded.
    /// </summary>
    public bool GtinLookupAttempted { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

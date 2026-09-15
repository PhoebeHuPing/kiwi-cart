using KiwiCart.Core.DTOs;

namespace KiwiCart.Core.Interfaces;

public interface IPriceComparisonService
{
    Task<IReadOnlyList<PriceResult>> CompareAsync(
        string searchTerm, CancellationToken ct = default,
        double? lat = null, double? lng = null);

    /// <summary>
    /// Compare prices for a product by GTIN (barcode) across all supermarkets.
    /// GTIN matching is exact and cross-platform, ensuring the same physical
    /// product is matched across Woolworths, Pak'nSave, and New World.
    /// </summary>
    Task<IReadOnlyList<PriceResult>> CompareByGtinAsync(
        string gtin, CancellationToken ct = default,
        double? lat = null, double? lng = null);
}

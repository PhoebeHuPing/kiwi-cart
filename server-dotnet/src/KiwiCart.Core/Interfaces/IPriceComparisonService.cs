using KiwiCart.Core.DTOs;

namespace KiwiCart.Core.Interfaces;

public interface IPriceComparisonService
{
    Task<IReadOnlyList<PriceResult>> CompareAsync(string searchTerm, CancellationToken ct = default);
}

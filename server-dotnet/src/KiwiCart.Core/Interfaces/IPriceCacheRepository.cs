using KiwiCart.Core.DTOs;

namespace KiwiCart.Core.Interfaces;

public interface IPriceCacheRepository
{
    Task<IReadOnlyList<PriceResult>> GetCachedPricesAsync(string searchTerm, CancellationToken ct = default);
    Task UpsertPriceAsync(PriceResult price, CancellationToken ct = default);
}

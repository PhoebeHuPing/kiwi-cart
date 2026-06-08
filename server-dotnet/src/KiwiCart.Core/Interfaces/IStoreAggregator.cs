using KiwiCart.Core.DTOs;

namespace KiwiCart.Core.Interfaces;

public interface IStoreAggregator
{
    Task<IReadOnlyList<PriceResult>> SearchAllStoresAsync(string term, CancellationToken ct = default);
}

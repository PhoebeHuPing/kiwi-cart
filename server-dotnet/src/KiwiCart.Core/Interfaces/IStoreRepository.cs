using KiwiCart.Core.DTOs;
using KiwiCart.Core.Entities;

namespace KiwiCart.Core.Interfaces;

public interface IStoreRepository
{
    Task<StoreUpsertResult> UpsertStoresAsync(
        IReadOnlyList<StoreInfo> stores, CancellationToken ct = default);

    Task<Store?> GetNearestStoreWithExternalIdAsync(
        string brand, double lat, double lng, double radiusKm, CancellationToken ct = default);
}

public record StoreUpsertResult(int Inserted, int Updated);

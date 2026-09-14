using KiwiCart.Core.DTOs;

namespace KiwiCart.Core.Interfaces;

public interface IStoreRepository
{
    /// <summary>
    /// Upsert stores by (brand, external_store_id): insert new stores, and
    /// refresh name/address/coordinates for existing ones. Returns the number
    /// of rows inserted and updated.
    /// </summary>
    Task<StoreUpsertResult> UpsertStoresAsync(
        IReadOnlyList<StoreInfo> stores, CancellationToken ct = default);
}

public record StoreUpsertResult(int Inserted, int Updated);

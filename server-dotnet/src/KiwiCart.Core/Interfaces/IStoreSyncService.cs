using KiwiCart.Core.Interfaces;

namespace KiwiCart.Core.Interfaces;

/// <summary>
/// Admin-only sync of retailer store lists into the stores table.
/// </summary>
public interface IStoreSyncService
{
    /// <summary>
    /// Fetch all Pak'nSave stores and upsert them. Returns insert/update counts.
    /// </summary>
    Task<StoreUpsertResult> SyncPakNSaveStoresAsync(CancellationToken ct = default);

    /// <summary>
    /// Fetch all New World stores and upsert them. Returns insert/update counts.
    /// </summary>
    Task<StoreUpsertResult> SyncNewWorldStoresAsync(CancellationToken ct = default);
}

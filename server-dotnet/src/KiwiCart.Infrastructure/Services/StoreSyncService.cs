using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.StoreClients;

namespace KiwiCart.Infrastructure.Services;

public class StoreSyncService : IStoreSyncService
{
    private readonly PakNSaveClient _pakNSave;
    private readonly NewWorldClient _newWorld;
    private readonly IStoreRepository _repository;

    public StoreSyncService(PakNSaveClient pakNSave, NewWorldClient newWorld, IStoreRepository repository)
    {
        _pakNSave = pakNSave;
        _newWorld = newWorld;
        _repository = repository;
    }

    public async Task<StoreUpsertResult> SyncPakNSaveStoresAsync(CancellationToken ct = default)
    {
        var stores = await _pakNSave.FetchStoresAsync(ct);
        return await _repository.UpsertStoresAsync(stores, ct);
    }

    public async Task<StoreUpsertResult> SyncNewWorldStoresAsync(CancellationToken ct = default)
    {
        var stores = await _newWorld.FetchStoresAsync(ct);
        return await _repository.UpsertStoresAsync(stores, ct);
    }
}

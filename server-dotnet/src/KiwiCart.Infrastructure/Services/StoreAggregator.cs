using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.StoreClients;
using Microsoft.Extensions.Logging;

namespace KiwiCart.Infrastructure.Services;

public class StoreAggregator : IStoreAggregator
{
    private readonly IEnumerable<StoreApiClient> _clients;
    private readonly ILogger<StoreAggregator> _logger;

    public StoreAggregator(IEnumerable<StoreApiClient> clients, ILogger<StoreAggregator> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public IReadOnlyCollection<string> KnownStoreBrands =>
        _clients.Select(c => c.StoreBrand).ToList();

    public async Task<IReadOnlyList<PriceResult>> SearchAllStoresAsync(
        string term, CancellationToken ct = default,
        IReadOnlyDictionary<string, string>? storeIdsByBrand = null)
    {
        var tasks = _clients.Select(client => SearchStoreAsync(client, term, ct, storeIdsByBrand));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    public async Task<IReadOnlyList<PriceResult>> SearchStoresAsync(
        IReadOnlyCollection<string> storeBrands, string term, CancellationToken ct = default,
        IReadOnlyDictionary<string, string>? storeIdsByBrand = null)
    {
        var tasks = _clients
            .Where(c => storeBrands.Contains(c.StoreBrand))
            .Select(client => SearchStoreAsync(client, term, ct, storeIdsByBrand));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    public async Task<IReadOnlyList<PriceResult>> SearchByGtinAsync(
        IReadOnlyCollection<string> storeBrands, string gtin, CancellationToken ct = default,
        IReadOnlyDictionary<string, string>? storeIdsByBrand = null)
    {
        var tasks = _clients
            .Where(c => storeBrands.Contains(c.StoreBrand))
            .Select(client => SearchByGtinStoreAsync(client, gtin, ct, storeIdsByBrand));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    private async Task<IReadOnlyList<PriceResult>> SearchStoreAsync(
        StoreApiClient client, string term, CancellationToken ct,
        IReadOnlyDictionary<string, string>? storeIdsByBrand = null)
    {
        try
        {
            // Use a caller-selected store id for this brand if provided.
            string? storeId = null;
            storeIdsByBrand?.TryGetValue(client.StoreBrand, out storeId);

            var results = await client.SearchAsync(term, ct, storeId);
            if (results.Count == 0)
                _logger.LogWarning("{Store}: returned no results for '{Term}'", client.StoreName, term);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Store}: failed during aggregated search for '{Term}', excluding from results",
                client.StoreName, term);
            return [];
        }
    }

    private async Task<IReadOnlyList<PriceResult>> SearchByGtinStoreAsync(
        StoreApiClient client, string gtin, CancellationToken ct,
        IReadOnlyDictionary<string, string>? storeIdsByBrand = null)
    {
        try
        {
            string? storeId = null;
            storeIdsByBrand?.TryGetValue(client.StoreBrand, out storeId);

            var results = await client.SearchByGtinAsync(gtin, ct, storeId);
            if (results.Count == 0)
                _logger.LogWarning("{Store}: returned no results for GTIN '{Gtin}'", client.StoreName, gtin);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Store}: failed during GTIN search for '{Gtin}', excluding from results",
                client.StoreName, gtin);
            return [];
        }
    }
}

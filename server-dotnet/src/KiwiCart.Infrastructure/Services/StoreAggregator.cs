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

    public async Task<IReadOnlyList<PriceResult>> SearchAllStoresAsync(string term, CancellationToken ct = default)
    {
        var tasks = _clients.Select(client => SearchStoreAsync(client, term, ct));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    private async Task<IReadOnlyList<PriceResult>> SearchStoreAsync(
        StoreApiClient client, string term, CancellationToken ct)
    {
        try
        {
            var results = await client.SearchAsync(term, ct);
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
}

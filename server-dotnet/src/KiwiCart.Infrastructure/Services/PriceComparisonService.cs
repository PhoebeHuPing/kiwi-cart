using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KiwiCart.Infrastructure.Services;

public class PriceComparisonService : IPriceComparisonService
{
    private readonly IPriceCacheRepository _cache;
    private readonly IStoreAggregator _aggregator;
    private readonly IPriceCalculator _calculator;
    private readonly ILogger<PriceComparisonService> _logger;

    public PriceComparisonService(
        IPriceCacheRepository cache,
        IStoreAggregator aggregator,
        IPriceCalculator calculator,
        ILogger<PriceComparisonService> logger)
    {
        _cache = cache;
        _aggregator = aggregator;
        _calculator = calculator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PriceResult>> CompareAsync(string searchTerm, CancellationToken ct = default)
    {
        // Try cache first
        var cached = await _cache.GetCachedPricesAsync(searchTerm, ct);
        if (cached.Count > 0)
        {
            foreach (var r in cached)
                r.UnitPrice = _calculator.CalculateUnitPrice(r.ProductName, r.Price);
            return cached.OrderBy(r => r.Price).ToList();
        }

        // Cache miss — fetch from all stores in parallel
        var results = await _aggregator.SearchAllStoresAsync(searchTerm, ct);

        // Calculate unit prices
        foreach (var r in results)
            r.UnitPrice = _calculator.CalculateUnitPrice(r.ProductName, r.Price);

        // Background cache upsert (non-blocking)
        _ = Task.Run(async () =>
        {
            foreach (var r in results)
            {
                try { await _cache.UpsertPriceAsync(r, CancellationToken.None); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to cache price for {Product}", r.ProductName); }
            }
        }, CancellationToken.None);

        return results.OrderBy(r => r.Price).ToList();
    }
}

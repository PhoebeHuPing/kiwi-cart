using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;

namespace KiwiCart.Infrastructure.Services;

public class BucketService : IBucketService
{
    private readonly IStoreAggregator _aggregator;

    public BucketService(IStoreAggregator aggregator)
    {
        _aggregator = aggregator;
    }

    public async Task<IReadOnlyList<BucketCompareResult>> CompareAsync(
        List<BucketItemInput> items, CancellationToken ct = default)
    {
        // Fetch prices for all items in parallel (bounded)
        var semaphore = new SemaphoreSlim(3);
        var tasks = items.Select(async item =>
        {
            await semaphore.WaitAsync(ct);
            try { return (item, results: await _aggregator.SearchAllStoresAsync(item.Name, ct)); }
            finally { semaphore.Release(); }
        });

        var allResults = await Task.WhenAll(tasks);

        // Group by store, compute totals
        var storeNames = allResults
            .SelectMany(r => r.results)
            .Select(r => r.StoreName)
            .Distinct();

        var comparison = new List<BucketCompareResult>();

        foreach (var store in storeNames)
        {
            var result = new BucketCompareResult { StoreName = store };
            var storeMatch = allResults
                .SelectMany(r => r.results)
                .FirstOrDefault(r => r.StoreName == store);

            if (storeMatch is not null)
            {
                result.LogoUrl = storeMatch.LogoUrl;
            }

            foreach (var (item, results) in allResults)
            {
                // Pick cheapest match for this item at this store
                var match = results
                    .Where(r => r.StoreName == store)
                    .OrderBy(r => r.Price)
                    .FirstOrDefault();

                if (match is not null)
                {
                    var subtotal = match.Price * item.Quantity;
                    result.TotalPrice += subtotal;
                    result.ItemsFound++;
                    result.Details.Add(new BucketItemDetail
                    {
                        Name = item.Name,
                        Price = match.Price,
                        Quantity = item.Quantity,
                        Subtotal = subtotal
                    });
                }
                else
                {
                    result.MissingItems.Add(item.Name);
                }
            }

            comparison.Add(result);
        }

        return comparison
            .OrderByDescending(r => r.ItemsFound)
            .ThenBy(r => r.TotalPrice)
            .ToList();
    }
}

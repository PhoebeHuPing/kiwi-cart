using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using System.Text.RegularExpressions;

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
                    .Where(r => IsReliableMatch(item, r))
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
                        MatchedProductName = match.ProductName,
                        MatchType = GetMatchType(item, match),
                        Brand = match.Brand,
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

    private static bool IsReliableMatch(BucketItemInput item, PriceResult result)
    {
        var hasIdentity = item.Gtins.Count > 0 || item.ProductIds.Count > 0;
        if (hasIdentity)
        {
            var gtinMatch = !string.IsNullOrWhiteSpace(result.Gtin)
                && item.Gtins.Contains(result.Gtin, StringComparer.OrdinalIgnoreCase);
            var productIdMatch = !string.IsNullOrWhiteSpace(result.ProductId)
                && item.ProductIds.Contains(result.ProductId, StringComparer.OrdinalIgnoreCase);
            return gtinMatch || productIdMatch;
        }

        var requestedWords = Words(item.Name);
        var resultWords = Words($"{result.ProductName} {result.DisplayProductName}");
        return requestedWords.All(resultWords.Contains);
    }

    private static string GetMatchType(BucketItemInput item, PriceResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Gtin)
            && item.Gtins.Contains(result.Gtin, StringComparer.OrdinalIgnoreCase))
            return "gtin";
        if (!string.IsNullOrWhiteSpace(result.ProductId)
            && item.ProductIds.Contains(result.ProductId, StringComparer.OrdinalIgnoreCase))
            return "product_id";
        return "name";
    }

    private static HashSet<string> Words(string value)
        => Regex.Split(value.ToLowerInvariant(), @"[^a-z0-9]+")
            .Where(word => word.Length > 1)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}

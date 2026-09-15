using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace KiwiCart.Tests.Services;

public class PriceComparisonServiceTests
{
    private readonly Mock<IPriceCacheRepository> _cache = new();
    private readonly Mock<IStoreAggregator> _aggregator = new();
    private readonly Mock<IPriceCalculator> _calculator = new();
    private readonly Mock<IProductGtinRepository> _gtins = new();
    private readonly Mock<IStoreService> _stores = new();
    private readonly PriceComparisonService _sut;

    private static readonly string[] AllBrands = { "PakNSave", "NewWorld", "Woolworths" };

    public PriceComparisonServiceTests()
    {
        _aggregator.SetupGet(a => a.KnownStoreBrands).Returns(AllBrands);
        _calculator.Setup(c => c.CalculateUnitPrice(It.IsAny<string>(), It.IsAny<decimal>()))
            .Returns("");
        // Default: no GTINs resolved unless a test sets them up.
        _gtins.Setup(g => g.GetGtinsForAsync(
                It.IsAny<IReadOnlyCollection<(string, string)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());
        _sut = new PriceComparisonService(
            _cache.Object, _aggregator.Object, _calculator.Object, _gtins.Object,
            _stores.Object, NullLogger<PriceComparisonService>.Instance);
    }

    [Fact]
    public async Task CompareAsync_CacheBelowMinimumFetchesAllStores()
    {
        // A row for every store is not enough; the cache must have at least
        // 100 results per store before it can bypass live search.
        var cached = new List<PriceResult>
        {
            new() { ProductName = "Milk", StoreBrand = "PakNSave", StoreName = "Pak'nSave", Price = 3.50m },
            new() { ProductName = "Milk", StoreBrand = "NewWorld", StoreName = "New World", Price = 3.90m },
            new() { ProductName = "Milk", StoreBrand = "Woolworths", StoreName = "Woolworths", Price = 4.00m }
        };
        _cache.Setup(c => c.GetCachedPricesAsync("Milk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var results = await _sut.CompareAsync("Milk");

        Assert.Equal(3, results.Count);
        Assert.Equal(3.50m, results[0].Price); // sorted cheapest first
        _aggregator.Verify(a => a.SearchStoresAsync(
            It.Is<IReadOnlyCollection<string>>(brands => brands.Count == 3),
            "Milk", It.IsAny<CancellationToken>(),
            It.IsAny<IReadOnlyDictionary<string, string>?>()), Times.Once);
    }

    [Fact]
    public async Task CompareAsync_EnrichesResultsWithGtin()
    {
        var cached = new List<PriceResult>
        {
            new() { ProductName = "Blue Milk", StoreBrand = "PakNSave", StoreName = "Pak'nSave", ProductId = "5000527-EA-000", Price = 3.60m },
            new() { ProductName = "milk standard blue", StoreBrand = "Woolworths", StoreName = "Woolworths", ProductId = "282819", Price = 3.70m },
            new() { ProductName = "Blue Milk", StoreBrand = "NewWorld", StoreName = "New World", ProductId = "5000527-EA-000", Price = 3.90m }
        };
        _cache.Setup(c => c.GetCachedPricesAsync("milk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        // Both the Foodstuffs and Woolworths ids resolve to the same GTIN.
        _gtins.Setup(g => g.GetGtinsForAsync(
                It.IsAny<IReadOnlyCollection<(string, string)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>
            {
                ["PakNSave|5000527-EA-000"] = "00000094152210",
                ["NewWorld|5000527-EA-000"] = "00000094152210",
                ["Woolworths|282819"] = "00000094152210",
            });

        var results = await _sut.CompareAsync("milk");

        Assert.Equal(3, results.Count);
        // Every row is enriched with the same GTIN so the frontend can merge them.
        Assert.All(results, r => Assert.Equal("00000094152210", r.Gtin));
    }

    [Fact]
    public async Task CompareAsync_FullCacheMiss_FetchesAllStores()
    {
        _cache.Setup(c => c.GetCachedPricesAsync("Bread", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult>());
        var fresh = new List<PriceResult>
        {
            new() { ProductName = "Bread", StoreBrand = "NewWorld", Price = 4.50m },
            new() { ProductName = "Bread", StoreBrand = "PakNSave", Price = 3.80m }
        };
        _aggregator.Setup(a => a.SearchStoresAsync(
                It.Is<IReadOnlyCollection<string>>(b => b.Count == 3),
                "Bread", It.IsAny<CancellationToken>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(fresh);

        var results = await _sut.CompareAsync("Bread");

        Assert.Equal(2, results.Count);
        Assert.Equal(3.80m, results[0].Price);
    }

    [Fact]
    public async Task CompareAsync_MissingExternalStoreIds_DoesNotDropBrands()
    {
        _stores.Setup(s => s.GetNearestStoreWithExternalIdAsync(
                It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KiwiCart.Core.Entities.Store?)null);
        _cache.Setup(c => c.GetCachedPricesAsync("Milk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult>());
        _aggregator.Setup(a => a.SearchStoresAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                "Milk", It.IsAny<CancellationToken>(),
                It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(new List<PriceResult>());

        await _sut.CompareAsync("Milk", lat: -36.85, lng: 174.76);

        _aggregator.Verify(a => a.SearchStoresAsync(
            It.IsAny<IReadOnlyCollection<string>>(),
            "Milk", It.IsAny<CancellationToken>(),
            It.IsAny<IReadOnlyDictionary<string, string>?>()), Times.Once);
    }

    [Fact]
    public async Task CompareAsync_PartialCache_RefreshesAllStores()
    {
        // A partial cache must trigger a live refresh for every store so
        // paginated results are not hidden by stale rows.
        _cache.Setup(c => c.GetCachedPricesAsync("Calci-Yum", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult>
            {
                new() { ProductName = "Calci-Yum", StoreBrand = "PakNSave", StoreName = "Pak'nSave", Price = 1.19m }
            });

        var fresh = new List<PriceResult>
        {
            new() { ProductName = "Calci-Yum", StoreBrand = "PakNSave", StoreName = "Pak'nSave", Price = 1.20m },
            new() { ProductName = "Calci-Yum", StoreBrand = "NewWorld", StoreName = "New World", Price = 1.49m },
            new() { ProductName = "Calci-Yum", StoreBrand = "Woolworths", StoreName = "Woolworths", Price = 1.60m }
        };
        _aggregator.Setup(a => a.SearchStoresAsync(
                It.Is<IReadOnlyCollection<string>>(b => b.Count == 3),
                "Calci-Yum", It.IsAny<CancellationToken>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(fresh);

        var results = await _sut.CompareAsync("Calci-Yum");

        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.StoreBrand == "PakNSave");
        Assert.Contains(results, r => r.StoreBrand == "NewWorld");
        Assert.Contains(results, r => r.StoreBrand == "Woolworths");
        Assert.Equal(1.20m, results[0].Price); // sorted cheapest first
    }

    [Fact]
    public async Task CompareAsync_ResultsSortedByPrice()
    {
        _cache.Setup(c => c.GetCachedPricesAsync("Eggs", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult>());
        _aggregator.Setup(a => a.SearchStoresAsync(
                It.IsAny<IReadOnlyCollection<string>>(), "Eggs", It.IsAny<CancellationToken>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(new List<PriceResult>
            {
                new() { ProductName = "Eggs", StoreBrand = "C", Price = 9.00m },
                new() { ProductName = "Eggs", StoreBrand = "A", Price = 5.00m },
                new() { ProductName = "Eggs", StoreBrand = "B", Price = 7.00m }
            });

        var results = await _sut.CompareAsync("Eggs");

        Assert.Equal(5.00m, results[0].Price);
        Assert.Equal(7.00m, results[1].Price);
        Assert.Equal(9.00m, results[2].Price);
    }
}

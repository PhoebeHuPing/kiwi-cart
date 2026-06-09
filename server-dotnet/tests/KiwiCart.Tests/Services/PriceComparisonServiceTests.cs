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
    private readonly PriceComparisonService _sut;

    public PriceComparisonServiceTests()
    {
        _sut = new PriceComparisonService(
            _cache.Object, _aggregator.Object, _calculator.Object,
            NullLogger<PriceComparisonService>.Instance);
    }

    [Fact]
    public async Task CompareAsync_CacheHit_ReturnsCachedResults()
    {
        var cached = new List<PriceResult>
        {
            new() { ProductName = "Milk", StoreName = "PakNSave", Price = 3.50m },
            new() { ProductName = "Milk", StoreName = "Woolworths", Price = 4.00m }
        };
        _cache.Setup(c => c.GetCachedPricesAsync("Milk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);
        _calculator.Setup(c => c.CalculateUnitPrice(It.IsAny<string>(), It.IsAny<decimal>()))
            .Returns("$3.50/L");

        var results = await _sut.CompareAsync("Milk");

        Assert.Equal(2, results.Count);
        Assert.Equal(3.50m, results[0].Price); // sorted cheapest first
        _aggregator.Verify(a => a.SearchAllStoresAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompareAsync_CacheMiss_FetchesFromAggregator()
    {
        _cache.Setup(c => c.GetCachedPricesAsync("Bread", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult>());
        var fresh = new List<PriceResult>
        {
            new() { ProductName = "Bread", StoreName = "NewWorld", Price = 4.50m },
            new() { ProductName = "Bread", StoreName = "PakNSave", Price = 3.80m }
        };
        _aggregator.Setup(a => a.SearchAllStoresAsync("Bread", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fresh);
        _calculator.Setup(c => c.CalculateUnitPrice(It.IsAny<string>(), It.IsAny<decimal>()))
            .Returns("");

        var results = await _sut.CompareAsync("Bread");

        Assert.Equal(2, results.Count);
        Assert.Equal(3.80m, results[0].Price);
    }

    [Fact]
    public async Task CompareAsync_ResultsSortedByPrice()
    {
        _cache.Setup(c => c.GetCachedPricesAsync("Eggs", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult>());
        _aggregator.Setup(a => a.SearchAllStoresAsync("Eggs", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult>
            {
                new() { ProductName = "Eggs", StoreName = "C", Price = 9.00m },
                new() { ProductName = "Eggs", StoreName = "A", Price = 5.00m },
                new() { ProductName = "Eggs", StoreName = "B", Price = 7.00m }
            });
        _calculator.Setup(c => c.CalculateUnitPrice(It.IsAny<string>(), It.IsAny<decimal>()))
            .Returns("");

        var results = await _sut.CompareAsync("Eggs");

        Assert.Equal(5.00m, results[0].Price);
        Assert.Equal(7.00m, results[1].Price);
        Assert.Equal(9.00m, results[2].Price);
    }
}

using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.Services;
using Moq;
using Xunit;

namespace KiwiCart.Tests.Services;

public class BucketServiceTests
{
    private readonly Mock<IStoreAggregator> _aggregator = new();
    private readonly BucketService _sut;

    public BucketServiceTests()
    {
        _sut = new BucketService(_aggregator.Object);
    }

    [Fact]
    public async Task CompareAsync_CalculatesTotalPerStore()
    {
        _aggregator.Setup(a => a.SearchAllStoresAsync("Milk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult>
            {
                new() { ProductName = "Milk", StoreName = "PakNSave", Price = 3.50m },
                new() { ProductName = "Milk", StoreName = "NewWorld", Price = 4.00m }
            });
        _aggregator.Setup(a => a.SearchAllStoresAsync("Bread", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult>
            {
                new() { ProductName = "Bread", StoreName = "PakNSave", Price = 2.00m },
                new() { ProductName = "Bread", StoreName = "NewWorld", Price = 2.50m }
            });

        var items = new List<BucketItemInput>
        {
            new() { Name = "Milk", Quantity = 1 },
            new() { Name = "Bread", Quantity = 2 }
        };

        var results = await _sut.CompareAsync(items);

        var paknsave = results.First(r => r.StoreName == "PakNSave");
        Assert.Equal(7.50m, paknsave.TotalPrice); // 3.50*1 + 2.00*2
        Assert.Equal(2, paknsave.ItemsFound);
    }

    [Fact]
    public async Task CompareAsync_TracksMissingItems()
    {
        _aggregator.Setup(a => a.SearchAllStoresAsync("Milk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult>
            {
                new() { ProductName = "Milk", StoreName = "PakNSave", Price = 3.50m }
            });
        _aggregator.Setup(a => a.SearchAllStoresAsync("Exotic Item", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult>
            {
                new() { ProductName = "Exotic Item", StoreName = "NewWorld", Price = 10.00m }
            });

        var items = new List<BucketItemInput>
        {
            new() { Name = "Milk", Quantity = 1 },
            new() { Name = "Exotic Item", Quantity = 1 }
        };

        var results = await _sut.CompareAsync(items);

        var paknsave = results.First(r => r.StoreName == "PakNSave");
        Assert.Contains("Exotic Item", paknsave.MissingItems);
        Assert.Equal(1, paknsave.ItemsFound);
    }

    [Fact]
    public async Task CompareAsync_SortsByItemsFoundThenPrice()
    {
        _aggregator.Setup(a => a.SearchAllStoresAsync("Milk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult>
            {
                new() { ProductName = "Milk", StoreName = "Full", Price = 5.00m },
                new() { ProductName = "Milk", StoreName = "Partial", Price = 3.00m }
            });
        _aggregator.Setup(a => a.SearchAllStoresAsync("Bread", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult>
            {
                new() { ProductName = "Bread", StoreName = "Full", Price = 2.00m }
                // Partial store doesn't have Bread
            });

        var items = new List<BucketItemInput>
        {
            new() { Name = "Milk", Quantity = 1 },
            new() { Name = "Bread", Quantity = 1 }
        };

        var results = await _sut.CompareAsync(items);

        Assert.Equal("Full", results[0].StoreName); // 2 items found > 1
    }
}

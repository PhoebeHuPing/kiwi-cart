using KiwiCart.Api.Controllers;
using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace KiwiCart.Tests.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IPriceComparisonService> _priceComparison = new();
    private readonly Mock<IBucketService> _bucketService = new();
    private readonly Mock<IFavoritesService> _favoritesService = new();
    private readonly Mock<IStoreService> _storeService = new();
    private readonly AppDbContext _db;
    private readonly ProductsController _sut;

    public ProductsControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("ControllerTest_" + Guid.NewGuid())
            .Options;
        _db = new AppDbContext(options);
        _sut = new ProductsController(
            _priceComparison.Object, _bucketService.Object,
            _favoritesService.Object, _storeService.Object, _db);
    }

    [Fact]
    public async Task Compare_EmptyQuery_ReturnsEmptyArray()
    {
        var result = await _sut.Compare("", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsAssignableFrom<PriceResult[]>(ok.Value);
        Assert.Empty(data);
    }

    [Fact]
    public async Task Compare_ValidQuery_ReturnsResults()
    {
        _priceComparison.Setup(p => p.CompareAsync("Milk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult> { new() { ProductName = "Milk", Price = 3.50m } });

        var result = await _sut.Compare("Milk", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsAssignableFrom<IReadOnlyList<PriceResult>>(ok.Value);
        Assert.Single(data);
    }

    [Fact]
    public async Task CompareBucket_EmptyItems_Returns400()
    {
        var request = new BucketCompareRequest { Items = [] };

        var result = await _sut.CompareBucket(request, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task CompareBucket_ItemWithEmptyName_Returns400()
    {
        var request = new BucketCompareRequest
        {
            Items = [new BucketItemInput { Name = "", Quantity = 1 }]
        };

        var result = await _sut.CompareBucket(request, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task CompareBucket_ValidRequest_ReturnsResults()
    {
        var expected = new List<BucketCompareResult>
        {
            new() { StoreName = "PakNSave", TotalPrice = 10.00m, ItemsFound = 2 }
        };
        _bucketService.Setup(b => b.CompareAsync(It.IsAny<List<BucketItemInput>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var request = new BucketCompareRequest
        {
            Items = [new BucketItemInput { Name = "Milk", Quantity = 1 }]
        };

        var result = await _sut.CompareBucket(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }
}

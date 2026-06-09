using KiwiCart.Api.Controllers;
using KiwiCart.Core.Entities;
using KiwiCart.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KiwiCart.Tests.Controllers;

public class StoresControllerTests
{
    private readonly Mock<IStoreService> _storeService = new();
    private readonly StoresController _sut;

    public StoresControllerTests()
    {
        _sut = new StoresController(_storeService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        _storeService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store> { new() { Id = 1, Name = "Test" } });

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsAssignableFrom<IReadOnlyList<Store>>(ok.Value);
        Assert.Single(data);
    }

    [Fact]
    public async Task GetNearby_MissingLat_Returns400()
    {
        var result = await _sut.GetNearby(null, 174.78);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task GetNearby_ValidParams_ReturnsResults()
    {
        _storeService.Setup(s => s.GetNearbyAsync(-41.29, 174.78, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreWithDistance>
            {
                new() { Name = "Nearby Store", DistanceKm = 1.2 }
            });

        var result = await _sut.GetNearby(-41.29, 174.78);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }
}

using KiwiCart.Core.Entities;
using KiwiCart.Infrastructure.Data;
using KiwiCart.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KiwiCart.Tests.Services;

public class StoreServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("StoreServiceTest_" + Guid.NewGuid())
            .Options;
        var db = new AppDbContext(options);
        db.Stores.AddRange(
            new Store { Id = 1, Name = "PakNSave Kilbirnie", Brand = "PakNSave", Latitude = -41.3267, Longitude = 174.8050, Address = "Kilbirnie" },
            new Store { Id = 2, Name = "New World Willis", Brand = "NewWorld", Latitude = -41.2920, Longitude = 174.7740, Address = "Willis St" },
            new Store { Id = 3, Name = "Woolworths Porirua", Brand = "Woolworths", Latitude = -41.1336, Longitude = 174.8406, Address = "Porirua" });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllStores()
    {
        using var db = CreateDb();
        var sut = new StoreService(db);

        var results = await sut.GetAllAsync();

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task GetNearbyAsync_FiltersWithinRadius()
    {
        using var db = CreateDb();
        var sut = new StoreService(db);

        // Point near Kilbirnie — should find Kilbirnie within 2km, not Porirua
        var results = await sut.GetNearbyAsync(-41.3267, 174.8050, radiusKm: 2);

        Assert.Single(results);
        Assert.Equal("PakNSave Kilbirnie", results[0].Name);
    }

    [Fact]
    public async Task GetNearbyAsync_SortsByDistance()
    {
        using var db = CreateDb();
        var sut = new StoreService(db);

        // Large radius to include all Wellington stores
        var results = await sut.GetNearbyAsync(-41.29, 174.78, radiusKm: 25);

        Assert.True(results[0].DistanceKm <= results[1].DistanceKm);
    }

    [Fact]
    public async Task GetNearbyAsync_InvalidCoordinates_ReturnsEmpty()
    {
        using var db = CreateDb();
        var sut = new StoreService(db);

        var results = await sut.GetNearbyAsync(-91, 200); // invalid

        Assert.Empty(results);
    }
}

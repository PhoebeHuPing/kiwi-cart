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
    public async Task GetNearestStoreWithExternalId_PicksClosestWithExternalId()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("Nearest_" + Guid.NewGuid()).Options;
        using var db = new AppDbContext(options);
        db.Stores.AddRange(
            // Far, has external id
            new Store { Id = 10, Name = "PNS Far", Brand = "PakNSave", Latitude = -36.9, Longitude = 174.9, Address = "Far", ExternalStoreId = "far-id" },
            // Near, but NO external id -> must be skipped
            new Store { Id = 11, Name = "PNS Near NoId", Brand = "PakNSave", Latitude = -41.30, Longitude = 174.80, Address = "Near", ExternalStoreId = null },
            // Near, has external id -> should be chosen
            new Store { Id = 12, Name = "PNS Near", Brand = "PakNSave", Latitude = -41.31, Longitude = 174.79, Address = "Near2", ExternalStoreId = "near-id" });
        db.SaveChanges();
        var sut = new StoreService(db);

        var store = await sut.GetNearestStoreWithExternalIdAsync("PakNSave", -41.30, 174.80);

        Assert.NotNull(store);
        Assert.Equal("near-id", store!.ExternalStoreId); // nearest one WITH an external id
    }

    [Fact]
    public async Task GetNearestStoreWithExternalId_ReturnsFallbackStore_WhenNoneHaveExternalId()
    {
        using var db = CreateDb(); // seed stores have no external ids
        var sut = new StoreService(db);

        var store = await sut.GetNearestStoreWithExternalIdAsync("PakNSave", -41.30, 174.80);

        // Should return a store even without external_store_id (fallback behavior)
        Assert.NotNull(store);
        Assert.Equal("PakNSave", store.Brand);
    }

    [Fact]
    public async Task GetNearestStoreWithExternalId_ReturnsNull_WhenNearestOutsideRadius()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("OutsideRadius_" + Guid.NewGuid()).Options;
        using var db = new AppDbContext(options);
        // Only store is ~15km away (outside the 5km NearbyRadiusKm).
        db.Stores.Add(new Store
        {
            Id = 20, Name = "PNS Far", Brand = "PakNSave",
            Latitude = -41.30, Longitude = 174.80, Address = "Far",
            ExternalStoreId = "far-id"
        });
        db.SaveChanges();
        var sut = new StoreService(db);

        // Query point ~15km north of the store.
        var store = await sut.GetNearestStoreWithExternalIdAsync("PakNSave", -41.165, 174.80);

        Assert.Null(store);
    }

    [Fact]
    public async Task GetNearestStoreWithExternalId_ReturnsStore_WhenWithinRadius()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("WithinRadius_" + Guid.NewGuid()).Options;
        using var db = new AppDbContext(options);
        db.Stores.Add(new Store
        {
            Id = 21, Name = "PNS Close", Brand = "PakNSave",
            Latitude = -41.30, Longitude = 174.80, Address = "Close",
            ExternalStoreId = "close-id"
        });
        db.SaveChanges();
        var sut = new StoreService(db);

        // Query point ~1km away, well within 5km.
        var store = await sut.GetNearestStoreWithExternalIdAsync("PakNSave", -41.291, 174.80);

        Assert.NotNull(store);
        Assert.Equal("close-id", store!.ExternalStoreId);
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

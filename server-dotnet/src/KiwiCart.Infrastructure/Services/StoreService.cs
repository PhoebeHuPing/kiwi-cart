using KiwiCart.Core.Entities;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KiwiCart.Infrastructure.Services;

public class StoreService : IStoreService
{
    private readonly AppDbContext _db;

    public StoreService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Store>> GetAllAsync(CancellationToken ct = default)
        => await _db.Stores.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<StoreWithDistance>> GetNearbyAsync(
        double lat, double lng, double radiusKm = 5, CancellationToken ct = default)
    {
        if (lat < -90 || lat > 90 || lng < -180 || lng > 180)
            return [];

        var stores = await _db.Stores.AsNoTracking().ToListAsync(ct);

        return stores
            .Select(s => new StoreWithDistance
            {
                Id = s.Id,
                Name = s.Name,
                Brand = s.Brand,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Address = s.Address,
                DistanceKm = Haversine(lat, lng, s.Latitude, s.Longitude)
            })
            .Where(s => s.DistanceKm <= radiusKm)
            .OrderBy(s => s.DistanceKm)
            .ToList();
    }

    private static double Haversine(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371; // Earth radius in km
        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}

using KiwiCart.Core.Entities;

namespace KiwiCart.Core.Interfaces;

public interface IStoreService
{
    Task<IReadOnlyList<Store>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StoreWithDistance>> GetNearbyAsync(double lat, double lng, double radiusKm = 5, CancellationToken ct = default);
}

public class StoreWithDistance
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
}

using KiwiCart.Core.Entities;

namespace KiwiCart.Core.Interfaces;

public interface IStoreService
{
    Task<IReadOnlyList<Store>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StoreWithDistance>> GetNearbyAsync(double lat, double lng, double radiusKm = 5, CancellationToken ct = default);

    /// <summary>
    /// Find the nearest store of the given brand to a location that has an
    /// external store id (i.e. can be queried for prices). Only considers stores
    /// within <paramref name="radiusKm"/> (default = nearby radius); pass
    /// double.MaxValue to ignore distance. Returns null when none qualifies.
    /// </summary>
    Task<Store?> GetNearestStoreWithExternalIdAsync(
        string brand, double lat, double lng, double radiusKm = 5, CancellationToken ct = default);
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

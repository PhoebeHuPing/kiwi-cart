namespace KiwiCart.Core.DTOs;

/// <summary>
/// A store fetched from a retailer's store-list endpoint, to be persisted into
/// the stores table.
/// </summary>
public record StoreInfo(
    string ExternalStoreId,
    string Name,
    string Brand,
    double Latitude,
    double Longitude,
    string Address);

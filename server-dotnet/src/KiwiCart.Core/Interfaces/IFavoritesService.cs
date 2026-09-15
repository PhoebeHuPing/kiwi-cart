namespace KiwiCart.Core.Interfaces;

public interface IFavoritesService
{
    Task<IReadOnlyList<string>> GetFavoritesAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<(string Name, string? Gtin)>> GetFavoritesWithGtinAsync(string userId, CancellationToken ct = default);
    Task<(string action, string name)> ToggleAsync(string userId, string productName, string? gtin = null, CancellationToken ct = default);
}

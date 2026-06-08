namespace KiwiCart.Core.Interfaces;

public interface IFavoritesService
{
    Task<IReadOnlyList<string>> GetFavoritesAsync(string userId, CancellationToken ct = default);
    Task<(string action, string name)> ToggleAsync(string userId, string productName, CancellationToken ct = default);
}

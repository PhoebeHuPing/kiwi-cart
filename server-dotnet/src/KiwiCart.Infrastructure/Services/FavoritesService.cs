using KiwiCart.Core.Entities;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KiwiCart.Infrastructure.Services;

public class FavoritesService : IFavoritesService
{
    private readonly AppDbContext _db;

    public FavoritesService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<string>> GetFavoritesAsync(string userId, CancellationToken ct = default)
    {
        return await _db.Favorites
            .Where(f => f.UserId == userId)
            .Join(_db.Products, f => f.ProductId, p => p.Id, (f, p) => p.Name)
            .ToListAsync(ct);
    }

    public async Task<(string action, string name)> ToggleAsync(string userId, string productName, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Name == productName, ct);
        if (product is null)
        {
            // Create product entry for favorite tracking
            product = new Product { Name = productName, Brand = "", Category = "" };
            _db.Products.Add(product);
            await _db.SaveChangesAsync(ct);
        }

        var existing = await _db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == product.Id, ct);

        if (existing is not null)
        {
            _db.Favorites.Remove(existing);
            await _db.SaveChangesAsync(ct);
            return ("removed", productName);
        }

        _db.Favorites.Add(new Favorite { UserId = userId, ProductId = product.Id });
        await _db.SaveChangesAsync(ct);
        return ("added", productName);
    }
}

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

    public async Task<IReadOnlyList<(string Name, string? Gtin)>> GetFavoritesWithGtinAsync(
        string userId, CancellationToken ct = default)
    {
        return await _db.Favorites
            .Where(f => f.UserId == userId)
            .Join(_db.Products, f => f.ProductId, p => p.Id, (f, p) => new { f.Gtin, p.Name })
            .Select(x => new { x.Name, x.Gtin })
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<(string, string?)>)t.Result
                .Select(x => (x.Name, x.Gtin))
                .ToList());
    }

    public async Task<(string action, string name)> ToggleAsync(string userId, string productName, string? gtin = null, CancellationToken ct = default)
    {
        // Use a transaction to ensure atomicity: find or create product, then toggle favorite.
        // This prevents race conditions where two requests for the same product both try to INSERT.
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.Name == productName, ct);
            
            if (product is null)
            {
                // Create product entry for favorite tracking.
                // Use INSERT ... ON CONFLICT to handle race conditions: if another request
                // inserted between our check and insert, the conflict clause makes this a no-op.
                product = new Product { Name = productName, Brand = "", Category = "" };
                _db.Products.Add(product);
                try
                {
                    await _db.SaveChangesAsync(ct);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                    when (ex.InnerException?.Message.Contains("duplicate key") == true)
                {
                    // Another request inserted the same product concurrently.
                    // Fetch it and continue.
                    _db.ChangeTracker.Clear();
                    product = await _db.Products
                        .FirstOrDefaultAsync(p => p.Name == productName, ct)
                        ?? throw new InvalidOperationException(
                            $"Product '{productName}' was not found after insertion failure.");
                }
            }

            var existing = await _db.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == product.Id, ct);

            if (existing is not null)
            {
                _db.Favorites.Remove(existing);
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return ("removed", productName);
            }

            _db.Favorites.Add(new Favorite { UserId = userId, ProductId = product.Id, Gtin = gtin });
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ("added", productName);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}

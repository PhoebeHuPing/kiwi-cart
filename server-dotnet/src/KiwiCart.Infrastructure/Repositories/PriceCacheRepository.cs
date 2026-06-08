using Dapper;
using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace KiwiCart.Infrastructure.Repositories;

public class PriceCacheRepository : IPriceCacheRepository
{
    private readonly string _connectionString;

    public PriceCacheRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured");
    }

    public async Task<IReadOnlyList<PriceResult>> GetCachedPricesAsync(string searchTerm, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<PriceResult>(
            @"SELECT p.name AS ProductName, s.name AS StoreName, s.brand AS StoreBrand,
                     pr.amount AS Price, pr.retrieved_at AS RetrievedAt
              FROM prices pr
              JOIN products p ON p.id = pr.product_id
              JOIN stores s ON s.id = pr.store_id
              WHERE p.name ILIKE @Term
                AND pr.retrieved_at > @Cutoff",
            new { Term = $"%{searchTerm}%", Cutoff = DateTime.UtcNow.AddHours(-24) });
        return results.ToList();
    }

    public async Task UpsertPriceAsync(PriceResult price, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        // Find or create product
        var productId = await connection.ExecuteScalarAsync<int?>(
            "SELECT id FROM products WHERE name = @Name AND brand = @Brand LIMIT 1",
            new { Name = price.ProductName, Brand = price.StoreBrand });

        if (productId is null)
        {
            productId = await connection.ExecuteScalarAsync<int>(
                "INSERT INTO products (name, brand, category) VALUES (@Name, @Brand, '') RETURNING id",
                new { Name = price.ProductName, Brand = price.StoreBrand });
        }

        // Find store
        var storeId = await connection.ExecuteScalarAsync<int?>(
            "SELECT id FROM stores WHERE brand = @Brand LIMIT 1",
            new { Brand = price.StoreBrand });

        if (storeId is null) return; // Store not seeded, skip caching

        // Upsert price
        await connection.ExecuteAsync(
            @"INSERT INTO prices (product_id, store_id, amount, retrieved_at)
              VALUES (@ProductId, @StoreId, @Price, @RetrievedAt)
              ON CONFLICT (product_id, store_id)
              DO UPDATE SET amount = @Price, retrieved_at = @RetrievedAt",
            new { ProductId = productId, StoreId = storeId, price.Price, price.RetrievedAt });
    }
}

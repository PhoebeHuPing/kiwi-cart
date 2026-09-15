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
            @"SELECT p.name AS ProductName, p.brand AS Brand, p.image_url AS ImageUrl, p.external_product_id AS ProductId,
                     s.name AS StoreName, s.brand AS StoreBrand,
                     s.address AS Address, s.latitude AS Lat, s.longitude AS Lng,
                     CASE s.brand
                        WHEN 'PakNSave'   THEN '/images/pak-n-save.webp'
                        WHEN 'NewWorld'   THEN '/images/new-world.webp'
                        WHEN 'Woolworths' THEN '/images/woolworths.webp'
                        ELSE NULL
                     END AS LogoUrl,
                     pr.amount AS Price, pr.retrieved_at AS RetrievedAt,
                     pr.volume AS Volume, pr.unit_price AS UnitPrice
              FROM prices pr
              JOIN products p ON p.id = pr.product_id
              JOIN stores s ON s.id = pr.store_id
              WHERE p.name ILIKE @Term
                AND pr.retrieved_at > @Cutoff",
            new { Term = $"%{searchTerm}%", Cutoff = DateTime.UtcNow.AddHours(-24) });
        
        // Regenerate DisplayProductName from brand + productName for cached rows.
        var resultList = results.ToList();
        foreach (var r in resultList)
        {
            if (string.IsNullOrEmpty(r.DisplayProductName))
            {
                r.DisplayProductName = BuildDisplayName(r.ProductName, r.Brand);
            }
        }
        
        return resultList;
    }

    public async Task<IReadOnlyList<PriceResult>> GetCachedPricesByGtinAsync(string gtin, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(gtin))
            return [];

        await using var connection = new NpgsqlConnection(_connectionString);

        // Join GTIN -> per-brand external_product_id -> products -> prices.
        // The `s.brand = pg.store_brand` constraint prevents Foodstuffs' shared
        // external_product_id (same id for PakNSave and NewWorld) from
        // cross-matching to the wrong store. DISTINCT ON keeps the most recent
        // price per store brand within the 24h cache window.
        var results = await connection.QueryAsync<PriceResult>(
            @"SELECT DISTINCT ON (s.brand)
                     p.name AS ProductName, p.brand AS Brand, p.image_url AS ImageUrl,
                     p.external_product_id AS ProductId, pg.gtin AS Gtin,
                     s.name AS StoreName, s.brand AS StoreBrand,
                     s.address AS Address, s.latitude AS Lat, s.longitude AS Lng,
                     CASE s.brand
                        WHEN 'PakNSave'   THEN '/images/pak-n-save.webp'
                        WHEN 'NewWorld'   THEN '/images/new-world.webp'
                        WHEN 'Woolworths' THEN '/images/woolworths.webp'
                        ELSE NULL
                     END AS LogoUrl,
                     pr.amount AS Price, pr.retrieved_at AS RetrievedAt,
                     pr.volume AS Volume, pr.unit_price AS UnitPrice
              FROM product_gtins pg
              JOIN products p ON p.external_product_id = pg.external_product_id
              JOIN prices pr ON pr.product_id = p.id
              JOIN stores s ON s.id = pr.store_id AND s.brand = pg.store_brand
              WHERE pg.gtin = @Gtin
                AND pr.retrieved_at > @Cutoff
              ORDER BY s.brand, pr.retrieved_at DESC",
            new { Gtin = gtin, Cutoff = DateTime.UtcNow.AddHours(-24) });

        var resultList = results.ToList();
        foreach (var r in resultList)
        {
            if (string.IsNullOrEmpty(r.DisplayProductName))
            {
                r.DisplayProductName = BuildDisplayName(r.ProductName, r.Brand);
            }
        }

        return resultList;
    }

    /// <summary>
    /// Build the display name for a cached product by prefixing the brand when
    /// the stored product name does not already start with it. Uses StartsWith
    /// (not Contains) so names that merely mention the brand mid-string still
    /// get the leading brand for consistent display, while names already led by
    /// the brand are left untouched (no duplication).
    /// </summary>
    internal static string BuildDisplayName(string productName, string? brand)
    {
        if (!string.IsNullOrEmpty(brand) &&
            !productName.StartsWith(brand, StringComparison.OrdinalIgnoreCase))
        {
            return $"{brand} {productName}";
        }

        return productName;
    }

    public async Task UpsertPriceAsync(PriceResult price, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        // Find or create product
        var productId = await connection.ExecuteScalarAsync<int?>(
            "SELECT id FROM products WHERE name = @Name AND brand = @Brand LIMIT 1",
            new { Name = price.ProductName, Brand = price.Brand ?? price.StoreBrand });

        if (productId is null)
        {
            productId = await connection.ExecuteScalarAsync<int>(
                "INSERT INTO products (name, brand, category, image_url, external_product_id) VALUES (@Name, @Brand, '', @ImageUrl, @ProductId) RETURNING id",
                new { Name = price.ProductName, Brand = price.Brand ?? price.StoreBrand, price.ImageUrl, price.ProductId });
        }
        else if (!string.IsNullOrEmpty(price.ImageUrl))
        {
            // Backfill/refresh image for an existing product when we have one.
            await connection.ExecuteAsync(
                "UPDATE products SET image_url = @ImageUrl WHERE id = @Id AND (image_url IS NULL OR image_url <> @ImageUrl)",
                new { price.ImageUrl, Id = productId });
        }

        // If we have a ProductId, update it for Foodstuffs product merging
        if (!string.IsNullOrEmpty(price.ProductId))
        {
            await connection.ExecuteAsync(
                "UPDATE products SET external_product_id = @ProductId WHERE id = @Id AND external_product_id IS NULL",
                new { price.ProductId, Id = productId });
        }

        // Find store
        var storeId = await connection.ExecuteScalarAsync<int?>(
            "SELECT id FROM stores WHERE brand = @Brand LIMIT 1",
            new { Brand = price.StoreBrand });

        if (storeId is null) return; // Store not seeded, skip caching

        // Upsert price with volume and unit_price
        await connection.ExecuteAsync(
            @"INSERT INTO prices (product_id, store_id, amount, retrieved_at, volume, unit_price)
              VALUES (@ProductId, @StoreId, @Price, @RetrievedAt, @Volume, @UnitPrice)
              ON CONFLICT (product_id, store_id)
              DO UPDATE SET amount = @Price, retrieved_at = @RetrievedAt, volume = @Volume, unit_price = @UnitPrice",
            new { ProductId = productId, StoreId = storeId, price.Price, price.RetrievedAt, price.Volume, price.UnitPrice });
    }
}

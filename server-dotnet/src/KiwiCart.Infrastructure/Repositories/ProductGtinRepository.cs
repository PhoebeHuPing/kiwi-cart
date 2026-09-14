using Dapper;
using KiwiCart.Core.DTOs;
using KiwiCart.Core.Entities;
using KiwiCart.Core.Interfaces;
using KiwiCart.Core.Utils;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace KiwiCart.Infrastructure.Repositories;

public class ProductGtinRepository : IProductGtinRepository
{
    private readonly string _connectionString;

    public ProductGtinRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured");
    }

    public async Task<bool> InsertIfAbsentAsync(PriceResult price, CancellationToken ct = default)
    {
        // Need a store brand, a platform product id, and a valid GTIN to record.
        if (string.IsNullOrEmpty(price.StoreBrand) || string.IsNullOrEmpty(price.ProductId))
            return false;

        var gtin = GtinNormalizer.Normalize(price.Gtin);
        if (gtin is null)
            return false;

        await using var connection = new NpgsqlConnection(_connectionString);

        // Insert only if this (store_brand, external_product_id) is not present.
        // ON CONFLICT DO NOTHING relies on the unique index and leaves any
        // existing row (including one a human has since edited) untouched.
        var now = DateTime.UtcNow;
        var rows = await connection.ExecuteAsync(
            @"INSERT INTO product_gtins
                  (store_brand, external_product_id, gtin, product_name,
                   product_brand, product_size, need_confirm, created_at, updated_at)
              VALUES (@StoreBrand, @ExternalProductId, @Gtin, @ProductName,
                      @ProductBrand, @ProductSize, false, @Now, @Now)
              ON CONFLICT (store_brand, external_product_id) DO NOTHING",
            new
            {
                price.StoreBrand,
                ExternalProductId = price.ProductId,
                Gtin = gtin,
                ProductName = price.ProductName,
                ProductBrand = price.Brand,
                ProductSize = price.Volume,
                Now = now
            });

        return rows > 0;
    }

    public async Task<GtinUpsertOutcome> UpsertForBackfillAsync(PriceResult price, CancellationToken ct = default)
    {
        // A store brand and platform product id are required to record a row.
        if (string.IsNullOrEmpty(price.StoreBrand) || string.IsNullOrEmpty(price.ProductId))
            return GtinUpsertOutcome.Skipped;

        // GTIN may legitimately be null here (row recorded for later backfill),
        // but if present it must be a valid GTIN.
        var gtin = GtinNormalizer.Normalize(price.Gtin);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var existingGtin = await connection.ExecuteScalarAsync<string?>(
            @"SELECT gtin FROM product_gtins
              WHERE store_brand = @StoreBrand AND external_product_id = @ExternalProductId",
            new { price.StoreBrand, ExternalProductId = price.ProductId });

        var now = DateTime.UtcNow;

        // No existing row -> insert (gtin may be null).
        if (existingGtin is null && !await RowExistsAsync(connection, price))
        {
            await connection.ExecuteAsync(
                @"INSERT INTO product_gtins
                      (store_brand, external_product_id, gtin, product_name,
                       product_brand, product_size, need_confirm, created_at, updated_at)
                  VALUES (@StoreBrand, @ExternalProductId, @Gtin, @ProductName,
                          @ProductBrand, @ProductSize, false, @Now, @Now)
                  ON CONFLICT (store_brand, external_product_id) DO NOTHING",
                new
                {
                    price.StoreBrand,
                    ExternalProductId = price.ProductId,
                    Gtin = gtin,
                    ProductName = price.ProductName,
                    ProductBrand = price.Brand,
                    ProductSize = price.Volume,
                    Now = now
                });
            return GtinUpsertOutcome.Inserted;
        }

        // Existing row already has a GTIN -> never overwrite it.
        if (!string.IsNullOrEmpty(existingGtin))
            return GtinUpsertOutcome.Skipped;

        // Existing row is missing a GTIN and we now have one -> backfill it.
        if (gtin is not null)
        {
            await connection.ExecuteAsync(
                @"UPDATE product_gtins
                  SET gtin = @Gtin,
                      product_name = COALESCE(@ProductName, product_name),
                      product_brand = COALESCE(@ProductBrand, product_brand),
                      product_size = COALESCE(@ProductSize, product_size),
                      updated_at = @Now
                  WHERE store_brand = @StoreBrand AND external_product_id = @ExternalProductId",
                new
                {
                    price.StoreBrand,
                    ExternalProductId = price.ProductId,
                    Gtin = gtin,
                    ProductName = price.ProductName,
                    ProductBrand = price.Brand,
                    ProductSize = price.Volume,
                    Now = now
                });
            return GtinUpsertOutcome.Updated;
        }

        // Existing row without a GTIN and we still don't have one -> nothing to do.
        return GtinUpsertOutcome.Skipped;
    }

    private static async Task<bool> RowExistsAsync(NpgsqlConnection connection, PriceResult price)
    {
        var count = await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(1) FROM product_gtins
              WHERE store_brand = @StoreBrand AND external_product_id = @ExternalProductId",
            new { price.StoreBrand, ExternalProductId = price.ProductId });
        return count > 0;
    }

    public async Task<IReadOnlyList<ProductGtin>> GetMissingGtinRowsAsync(
        IReadOnlyCollection<string> storeBrands, int limit, CancellationToken ct = default)
    {
        if (storeBrands.Count == 0 || limit <= 0)
            return [];

        await using var connection = new NpgsqlConnection(_connectionString);
        var rows = await connection.QueryAsync<ProductGtin>(
            @"SELECT id AS Id, store_brand AS StoreBrand, external_product_id AS ExternalProductId,
                     gtin AS Gtin, product_name AS ProductName, product_brand AS ProductBrand,
                     product_size AS ProductSize, need_confirm AS NeedConfirm,
                     gtin_lookup_attempted AS GtinLookupAttempted,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM product_gtins
              WHERE gtin IS NULL AND gtin_lookup_attempted = false AND store_brand = ANY(@StoreBrands)
              ORDER BY created_at
              LIMIT @Limit",
            new { StoreBrands = storeBrands.ToArray(), Limit = limit });
        return rows.ToList();
    }

    public async Task MarkGtinLookupAttemptedAsync(
        string storeBrand, string externalProductId, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            @"UPDATE product_gtins
              SET gtin_lookup_attempted = true, updated_at = @Now
              WHERE store_brand = @StoreBrand AND external_product_id = @ExternalProductId",
            new { StoreBrand = storeBrand, ExternalProductId = externalProductId, Now = DateTime.UtcNow });
    }

    public async Task<IReadOnlyDictionary<string, string>> GetGtinsForAsync(
        IReadOnlyCollection<(string StoreBrand, string ExternalProductId)> keys,
        CancellationToken ct = default)
    {
        var result = new Dictionary<string, string>();
        if (keys.Count == 0)
            return result;

        // Query candidate rows by the distinct brands and product ids involved,
        // then match exact pairs in memory. This keeps the SQL to two ANY(...)
        // filters rather than one row per pair.
        var brands = keys.Select(k => k.StoreBrand).Distinct().ToArray();
        var ids = keys.Select(k => k.ExternalProductId).Distinct().ToArray();

        await using var connection = new NpgsqlConnection(_connectionString);
        var rows = await connection.QueryAsync<(string StoreBrand, string ExternalProductId, string? Gtin)>(
            @"SELECT store_brand AS StoreBrand, external_product_id AS ExternalProductId, gtin AS Gtin
              FROM product_gtins
              WHERE gtin IS NOT NULL
                AND store_brand = ANY(@Brands)
                AND external_product_id = ANY(@Ids)",
            new { Brands = brands, Ids = ids });

        // Only keep exact (brand, id) pairs that were requested.
        var wanted = keys.Select(k => $"{k.StoreBrand}|{k.ExternalProductId}").ToHashSet();
        foreach (var r in rows)
        {
            if (string.IsNullOrEmpty(r.Gtin))
                continue;
            var key = $"{r.StoreBrand}|{r.ExternalProductId}";
            if (wanted.Contains(key))
                result[key] = r.Gtin;
        }
        return result;
    }
}

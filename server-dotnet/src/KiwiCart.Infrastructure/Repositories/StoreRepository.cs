using Dapper;
using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace KiwiCart.Infrastructure.Repositories;

public class StoreRepository : IStoreRepository
{
    private readonly string _connectionString;

    public StoreRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured");
    }

    public async Task<StoreUpsertResult> UpsertStoresAsync(
        IReadOnlyList<StoreInfo> stores, CancellationToken ct = default)
    {
        int inserted = 0, updated = 0;
        if (stores.Count == 0)
            return new StoreUpsertResult(0, 0);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        foreach (var s in stores)
        {
            // Does a row already exist for this (brand, external_store_id)?
            var existingId = await connection.ExecuteScalarAsync<int?>(
                @"SELECT id FROM stores
                  WHERE brand = @Brand AND external_store_id = @ExternalStoreId",
                new { s.Brand, s.ExternalStoreId });

            if (existingId is null)
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO stores (name, brand, latitude, longitude, address, external_store_id)
                      VALUES (@Name, @Brand, @Latitude, @Longitude, @Address, @ExternalStoreId)",
                    new { s.Name, s.Brand, s.Latitude, s.Longitude, s.Address, s.ExternalStoreId });
                inserted++;
            }
            else
            {
                await connection.ExecuteAsync(
                    @"UPDATE stores
                      SET name = @Name, latitude = @Latitude, longitude = @Longitude, address = @Address
                      WHERE id = @Id",
                    new { s.Name, s.Latitude, s.Longitude, s.Address, Id = existingId });
                updated++;
            }
        }

        return new StoreUpsertResult(inserted, updated);
    }
}

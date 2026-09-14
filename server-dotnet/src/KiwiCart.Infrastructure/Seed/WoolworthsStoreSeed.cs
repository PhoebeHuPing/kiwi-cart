using System.Text.Json;
using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;

namespace KiwiCart.Infrastructure.Seed;

/// <summary>
/// One-time importer for the Woolworths store list. The coordinates come from
/// cdx.nz, which is behind Akamai bot protection and only reachable from a real
/// browser — so the data is fetched client-side (scripts/ww-fetch-cdx.js) into a
/// JSON seed and imported here. Idempotent via StoreRepository.UpsertStoresAsync.
/// </summary>
public static class WoolworthsStoreSeed
{
    private sealed record SeedFile(List<SeedStore> Stores);

    private sealed record SeedStore(
        long SiteNo,
        string? Name,
        string? Division,
        string? AddressLine1,
        string? Suburb,
        string? Postcode,
        double Latitude,
        double Longitude);

    public static async Task<StoreUpsertResult> ImportAsync(
        IConfiguration configuration, string seedPath, CancellationToken ct = default)
    {
        if (!File.Exists(seedPath))
            throw new FileNotFoundException($"Woolworths seed not found: {seedPath}");

        await using var stream = File.OpenRead(seedPath);
        var seed = await JsonSerializer.DeserializeAsync<SeedFile>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);

        var stores = (seed?.Stores ?? [])
            // Only rows with usable coordinates.
            .Where(s => s.Latitude != 0 && s.Longitude != 0)
            .Select(s => new StoreInfo(
                ExternalStoreId: s.SiteNo.ToString(),
                Name: NormalizeName(s.Name),
                Brand: "Woolworths",
                Latitude: s.Latitude,
                Longitude: s.Longitude,
                Address: BuildAddress(s)))
            .ToList();

        var repo = new StoreRepository(configuration);
        return await repo.UpsertStoresAsync(stores, ct);
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Woolworths";
        var trimmed = name.Trim();
        // Ensure the brand is present so the frontend picks the right logo.
        return trimmed.Contains("Woolworths", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("Metro", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"Woolworths {trimmed}";
    }

    private static string BuildAddress(SeedStore s)
    {
        var parts = new[] { s.AddressLine1, s.Suburb, s.Postcode }
            .Where(p => !string.IsNullOrWhiteSpace(p) && p != "null");
        return string.Join(", ", parts);
    }
}

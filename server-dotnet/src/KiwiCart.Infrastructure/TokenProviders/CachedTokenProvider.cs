using System.Collections.Concurrent;
using KiwiCart.Core.DTOs;
using KiwiCart.Core.Entities;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KiwiCart.Infrastructure.TokenProviders;

public abstract class CachedTokenProvider : ITokenProvider
{
    private static readonly ConcurrentDictionary<string, StoreToken> _cache = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    protected CachedTokenProvider(IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public abstract string StoreName { get; }

    protected abstract Task<TokenResponse> FetchTokenAsync(CancellationToken ct);

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        // Layer 1: Static memory
        if (_cache.TryGetValue(StoreName, out var cached) && !cached.IsNearExpiry)
            return cached.Token;

        var semaphore = _locks.GetOrAdd(StoreName, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        try
        {
            // Double-check after lock
            if (_cache.TryGetValue(StoreName, out cached) && !cached.IsNearExpiry)
                return cached.Token;

            // Layer 2: DB
            var dbToken = await LoadFromDbAsync(ct);
            if (dbToken is not null && !dbToken.IsNearExpiry)
            {
                _cache[StoreName] = dbToken;
                return dbToken.Token;
            }

            // Layer 3: Store API
            _logger.LogInformation("Fetching new token from Store API for {StoreName}", StoreName);
            var response = await FetchTokenAsync(ct);
            var newToken = new StoreToken
            {
                StoreBrand = StoreName,
                Token = response.Token,
                IssuedAt = response.IssuedAt,
                ExpiresAt = response.ExpiresAt,
                UpdatedAt = DateTime.UtcNow
            };
            await SaveToDbAsync(newToken, ct);
            _cache[StoreName] = newToken;
            return newToken.Token;
        }
        finally { semaphore.Release(); }
    }

    private async Task<StoreToken?> LoadFromDbAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.StoreTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.StoreBrand == StoreName, ct);
    }

    private async Task SaveToDbAsync(StoreToken token, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var existing = await db.StoreTokens
                .FirstOrDefaultAsync(t => t.StoreBrand == StoreName, ct);

            if (existing is null)
            {
                db.StoreTokens.Add(token);
            }
            else
            {
                existing.Token = token.Token;
                existing.IssuedAt = token.IssuedAt;
                existing.ExpiresAt = token.ExpiresAt;
                existing.UpdatedAt = token.UpdatedAt;
            }

            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Unique constraint race (multi-instance): token is already in-memory cache, safe to continue
            _logger.LogWarning(ex, "Token DB upsert conflict for {StoreName}, using in-memory value", StoreName);
        }
    }

    // Exposed for testing
    internal static void ClearCache() => _cache.Clear();

    internal async Task InvalidateTokenAsync(string storeName, CancellationToken ct = default)
    {
        _cache.TryRemove(storeName, out _);
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.StoreTokens
            .FirstOrDefaultAsync(t => t.StoreBrand == storeName, ct);
        if (existing is not null)
        {
            db.StoreTokens.Remove(existing);
            await db.SaveChangesAsync(ct);
        }
    }
}

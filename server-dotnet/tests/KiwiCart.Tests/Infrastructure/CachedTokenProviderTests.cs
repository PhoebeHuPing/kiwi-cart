using KiwiCart.Core.DTOs;
using KiwiCart.Core.Entities;
using KiwiCart.Infrastructure.Data;
using KiwiCart.Infrastructure.TokenProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KiwiCart.Tests.Infrastructure;

public class CachedTokenProviderTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private int _fetchCount;
    private readonly string _dbName = Guid.NewGuid().ToString();

    public CachedTokenProviderTests()
    {
        var services = new ServiceCollection();
        var dbName = _dbName;
        services.AddDbContext<AppDbContext>(o =>
            o.UseInMemoryDatabase(dbName));
        _serviceProvider = services.BuildServiceProvider();
        CachedTokenProvider.ClearCache();
        _fetchCount = 0;
    }

    public void Dispose() => _serviceProvider.Dispose();

    private TestTokenProvider CreateProvider(DateTime? expiresAt = null)
    {
        var expires = expiresAt ?? DateTime.UtcNow.AddHours(1);
        return new TestTokenProvider(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLoggerFactory.Instance.CreateLogger("Test"),
            () =>
            {
                Interlocked.Increment(ref _fetchCount);
                return new TokenResponse("token-" + _fetchCount, DateTime.UtcNow, expires);
            });
    }

    [Fact]
    public async Task GetTokenAsync_ReturnsCachedToken_WhenStaticCacheValid()
    {
        var provider = CreateProvider();
        var first = await provider.GetTokenAsync();
        var second = await provider.GetTokenAsync();

        Assert.Equal(first, second);
        Assert.Equal(1, _fetchCount);
    }

    [Fact]
    public async Task GetTokenAsync_LoadsFromDb_WhenStaticCacheEmpty()
    {
        var expires = DateTime.UtcNow.AddHours(1);
        // Seed DB
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.StoreTokens.Add(new StoreToken
            {
                StoreBrand = "TestStore",
                Token = "db-token",
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = expires,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Static cache is already cleared in constructor
        var provider = CreateProvider(expires);
        var token = await provider.GetTokenAsync();

        Assert.Equal("db-token", token);
        Assert.Equal(0, _fetchCount);
    }

    [Fact]
    public async Task GetTokenAsync_FetchesFromApi_WhenBothEmpty()
    {
        var provider = CreateProvider();
        var token = await provider.GetTokenAsync();

        Assert.Equal("token-1", token);
        Assert.Equal(1, _fetchCount);
    }

    [Fact]
    public async Task GetTokenAsync_PersistsToDb_AfterApiFetch()
    {
        var provider = CreateProvider();
        await provider.GetTokenAsync();

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.StoreTokens.FirstOrDefaultAsync(t => t.StoreBrand == "TestStore");

        Assert.NotNull(stored);
        Assert.Equal("token-1", stored.Token);
    }

    [Fact]
    public async Task GetTokenAsync_PreventsConcurrentFetches()
    {
        var provider = CreateProvider();
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => provider.GetTokenAsync())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, _fetchCount);
        Assert.All(results, t => Assert.Equal("token-1", t));
    }

    [Fact]
    public async Task GetTokenAsync_TreatsNearExpiryAsExpired()
    {
        // First fetch with a valid expiry to populate DB
        var validExpiry = DateTime.UtcNow.AddHours(1);
        var provider = CreateProvider(validExpiry);
        await provider.GetTokenAsync();
        Assert.Equal(1, _fetchCount);

        // Now update DB to near-expiry and clear static cache
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var existing = await db.StoreTokens.FirstAsync(t => t.StoreBrand == "TestStore");
            existing.ExpiresAt = DateTime.UtcNow.AddMinutes(3); // within 5-min buffer
            await db.SaveChangesAsync();
        }

        CachedTokenProvider.ClearCache();

        // Create new provider that returns fresh token
        var provider2 = new TestTokenProvider(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLoggerFactory.Instance.CreateLogger("Test"),
            () =>
            {
                Interlocked.Increment(ref _fetchCount);
                return new TokenResponse("fresh-token", DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
            });

        var token = await provider2.GetTokenAsync();

        Assert.Equal(2, _fetchCount); // Should re-fetch since DB token is near expiry
        Assert.Equal("fresh-token", token);
    }
}

public class TestTokenProvider : CachedTokenProvider
{
    private readonly Func<TokenResponse> _fetchFunc;

    public TestTokenProvider(
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        Func<TokenResponse> fetchFunc)
        : base(scopeFactory, logger)
    {
        _fetchFunc = fetchFunc;
    }

    public override string StoreName => "TestStore";

    protected override Task<TokenResponse> FetchTokenAsync(CancellationToken ct)
        => Task.FromResult(_fetchFunc());
}

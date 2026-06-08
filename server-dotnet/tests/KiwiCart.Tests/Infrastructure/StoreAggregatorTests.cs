using KiwiCart.Core.DTOs;
using KiwiCart.Infrastructure.Data;
using KiwiCart.Infrastructure.Services;
using KiwiCart.Infrastructure.StoreClients;
using KiwiCart.Infrastructure.TokenProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace KiwiCart.Tests.Infrastructure;

public class StoreAggregatorTests
{
    [Fact]
    public async Task SearchAllStoresAsync_ReturnsResultsFromAllStores()
    {
        var client1 = new StubStoreClient("Store1", new[] { new PriceResult { ProductName = "Milk", StoreName = "Store1", Price = 3.50m } });
        var client2 = new StubStoreClient("Store2", new[] { new PriceResult { ProductName = "Milk", StoreName = "Store2", Price = 3.99m } });

        var aggregator = new StoreAggregator(
            new StoreApiClient[] { client1, client2 },
            NullLogger<StoreAggregator>.Instance);

        var results = await aggregator.SearchAllStoresAsync("Milk");

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.StoreName == "Store1");
        Assert.Contains(results, r => r.StoreName == "Store2");
    }

    [Fact]
    public async Task SearchAllStoresAsync_ExcludesFailedStore_ReturnsOthers()
    {
        var workingClient = new StubStoreClient("Working", new[] { new PriceResult { ProductName = "Bread", StoreName = "Working", Price = 2.00m } });
        var failingClient = new FailingStoreClient("Failing");

        var aggregator = new StoreAggregator(
            new StoreApiClient[] { workingClient, failingClient },
            NullLogger<StoreAggregator>.Instance);

        var results = await aggregator.SearchAllStoresAsync("Bread");

        Assert.Single(results);
        Assert.Equal("Working", results[0].StoreName);
    }

    [Fact]
    public async Task SearchAllStoresAsync_AllFail_ReturnsEmpty()
    {
        var failing1 = new FailingStoreClient("Store1");
        var failing2 = new FailingStoreClient("Store2");

        var aggregator = new StoreAggregator(
            new StoreApiClient[] { failing1, failing2 },
            NullLogger<StoreAggregator>.Instance);

        var results = await aggregator.SearchAllStoresAsync("Anything");

        Assert.Empty(results);
    }
}

internal class StubStoreClient : StoreApiClient
{
    private readonly PriceResult[] _results;

    public StubStoreClient(string name, PriceResult[] results)
        : base(CreateTokenProvider(), NullLogger<StubStoreClient>.Instance)
    {
        StoreName = name;
        _results = results;
    }

    public override string StoreName { get; }

    protected override Task<IReadOnlyList<PriceResult>?> ExecuteSearchAsync(
        string term, string token, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<PriceResult>?>(_results);

    private static CachedTokenProvider CreateTokenProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("StubDb" + Guid.NewGuid()));
        var sp = services.BuildServiceProvider();
        return new StubTokenProvider(sp.GetRequiredService<IServiceScopeFactory>());
    }
}

internal class FailingStoreClient : StoreApiClient
{
    public FailingStoreClient(string name)
        : base(CreateTokenProvider(), NullLogger<FailingStoreClient>.Instance)
    {
        StoreName = name;
    }

    public override string StoreName { get; }

    protected override Task<IReadOnlyList<PriceResult>?> ExecuteSearchAsync(
        string term, string token, CancellationToken ct)
        => throw new HttpRequestException("Store unreachable");

    private static CachedTokenProvider CreateTokenProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("FailDb" + Guid.NewGuid()));
        var sp = services.BuildServiceProvider();
        return new StubTokenProvider(sp.GetRequiredService<IServiceScopeFactory>());
    }
}

internal class StubTokenProvider : CachedTokenProvider
{
    public StubTokenProvider(IServiceScopeFactory scopeFactory)
        : base(scopeFactory, NullLogger<StubTokenProvider>.Instance) { }

    public override string StoreName => "Stub";

    protected override Task<TokenResponse> FetchTokenAsync(CancellationToken ct)
        => Task.FromResult(new TokenResponse("stub-token", DateTime.UtcNow, DateTime.UtcNow.AddHours(1)));
}

using System.Net;
using System.Text;
using System.Text.Json;
using KiwiCart.Core.DTOs;
using KiwiCart.Core.Entities;
using KiwiCart.Infrastructure.Data;
using KiwiCart.Infrastructure.StoreClients;
using KiwiCart.Infrastructure.TokenProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace KiwiCart.Tests.Infrastructure;

public class PakNSaveClientTests
{
    [Fact]
    public async Task SearchAsync_MapsProductsCorrectly()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            products = new[]
            {
                new { name = "Milk 2L", productId = "12345-abc", singlePrice = new { price = 599 } },
                new { name = "Bread Loaf", productId = "67890-def", singlePrice = new { price = 350 } }
            }
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, responseJson);
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api-prod.paknsave.co.nz") };
        var factory = CreateFactory("PakNSave", httpClient);

        var tokenProvider = new FakeTokenProvider("test-token");
        var client = new PakNSaveClient(tokenProvider, factory, NullLogger<PakNSaveClient>.Instance);

        var results = await client.SearchAsync("Milk");

        Assert.Equal(2, results.Count);
        Assert.Equal("Milk 2L", results[0].ProductName);
        Assert.Equal("Pak'nSave", results[0].StoreName);
        Assert.Equal(5.99m, results[0].Price);
        Assert.Equal("Bread Loaf", results[1].ProductName);
        Assert.Equal(3.50m, results[1].Price);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_OnFailure()
    {
        var handler = CreateMockHandler(HttpStatusCode.InternalServerError, "{}");
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api-prod.paknsave.co.nz") };
        var factory = CreateFactory("PakNSave", httpClient);

        var tokenProvider = new FakeTokenProvider("test-token");
        var client = new PakNSaveClient(tokenProvider, factory, NullLogger<PakNSaveClient>.Instance);

        var results = await client.SearchAsync("Milk");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_Retries_On401()
    {
        var callCount = 0;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { products = new[] { new { name = "Milk", productId = "1-a", singlePrice = new { price = 100 } } } }),
                        Encoding.UTF8, "application/json")
                };
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api-prod.paknsave.co.nz") };
        var factory = CreateFactory("PakNSave", httpClient);

        var tokenProvider = new FakeTokenProvider("token");
        var client = new PakNSaveClient(tokenProvider, factory, NullLogger<PakNSaveClient>.Instance);

        CachedTokenProvider.ClearCache();
        var results = await client.SearchAsync("Milk");

        Assert.Single(results);
        Assert.Equal(2, callCount); // First 401, then retry
    }

    private static Mock<HttpMessageHandler> CreateMockHandler(HttpStatusCode status, string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
        return handler;
    }

    private static IHttpClientFactory CreateFactory(string name, HttpClient client)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(name)).Returns(client);
        return factory.Object;
    }
}

public class FakeTokenProvider : CachedTokenProvider
{
    private readonly string _token;

    public FakeTokenProvider(string token)
        : base(CreateScopeFactory(), NullLoggerFactory.Instance.CreateLogger("Fake"))
    {
        _token = token;
    }

    public override string StoreName => "PakNSave";

    protected override Task<TokenResponse> FetchTokenAsync(CancellationToken ct)
        => Task.FromResult(new TokenResponse(_token, DateTime.UtcNow, DateTime.UtcNow.AddHours(1)));

    private static IServiceScopeFactory CreateScopeFactory()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("FakeTokenDb"));
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }
}

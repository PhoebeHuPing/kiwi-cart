using System.Net;
using System.Text;
using System.Text.Json;
using KiwiCart.Core.DTOs;
using KiwiCart.Infrastructure.Data;
using KiwiCart.Infrastructure.StoreClients;
using KiwiCart.Infrastructure.TokenProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace KiwiCart.Tests.Infrastructure;

public class NewWorldClientTests
{
    [Fact]
    public async Task SearchAsync_MapsProductsCorrectly()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            products = new[]
            {
                new { name = "Milk 2L", productId = "abc-123", singlePrice = new { price = 649 } },
                new { name = "Butter 500g", productId = "def-456", singlePrice = new { price = 489 } }
            }
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, responseJson);
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api-prod.newworld.co.nz") };
        var factory = CreateFactory("NewWorld", httpClient);

        var tokenProvider = new FakeNewWorldTokenProvider("test-token");
        var client = new NewWorldClient(tokenProvider, factory, NullLogger<NewWorldClient>.Instance);

        var results = await client.SearchAsync("Milk");

        Assert.Equal(2, results.Count);
        Assert.Equal("Milk 2L", results[0].ProductName);
        Assert.Equal("New World", results[0].StoreName);
        Assert.Equal("NewWorld", results[0].StoreBrand);
        Assert.Equal(6.49m, results[0].Price);
        Assert.Equal(4.89m, results[1].Price);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_OnFailure()
    {
        var handler = CreateMockHandler(HttpStatusCode.InternalServerError, "{}");
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api-prod.newworld.co.nz") };
        var factory = CreateFactory("NewWorld", httpClient);

        var tokenProvider = new FakeNewWorldTokenProvider("test-token");
        var client = new NewWorldClient(tokenProvider, factory, NullLogger<NewWorldClient>.Instance);

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
                        JsonSerializer.Serialize(new { products = new[] { new { name = "Bread", productId = "x-1", singlePrice = new { price = 450 } } } }),
                        Encoding.UTF8, "application/json")
                };
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api-prod.newworld.co.nz") };
        var factory = CreateFactory("NewWorld", httpClient);

        var tokenProvider = new FakeNewWorldTokenProvider("token");
        var client = new NewWorldClient(tokenProvider, factory, NullLogger<NewWorldClient>.Instance);

        CachedTokenProvider.ClearCache();
        var results = await client.SearchAsync("Bread");

        Assert.Single(results);
        Assert.Equal(4.50m, results[0].Price);
        Assert.Equal(2, callCount);
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

public class FakeNewWorldTokenProvider : NewWorldTokenProvider
{
    private readonly string _token;

    public FakeNewWorldTokenProvider(string token)
        : base(CreateScopeFactory(), NullLogger<NewWorldTokenProvider>.Instance,
            new Mock<IHttpClientFactory>().Object)
    {
        _token = token;
    }

    protected override Task<TokenResponse> FetchTokenAsync(CancellationToken ct)
        => Task.FromResult(new TokenResponse(_token, DateTime.UtcNow, DateTime.UtcNow.AddHours(1)));

    private static IServiceScopeFactory CreateScopeFactory()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("FakeNewWorldDb"));
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }
}

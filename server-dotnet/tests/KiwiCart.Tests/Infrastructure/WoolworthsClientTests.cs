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

public class WoolworthsClientTests
{
    [Fact]
    public async Task SearchAsync_MapsProductsCorrectly()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            products = new
            {
                items = new object[]
                {
                    new { name = "Milk 2L", type = "Product", price = new { salePrice = 3.99 } },
                    new { name = "Bread Toast", type = "Product", price = new { salePrice = 2.49 } },
                    new { name = "Banner Ad", type = "Banner", price = new { salePrice = 0 } }
                }
            }
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, responseJson);
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://www.woolworths.co.nz") };
        var factory = CreateFactory("Woolworths", httpClient);

        var tokenProvider = new FakeWoolworthsTokenProvider("session-cookie");
        var client = new WoolworthsClient(tokenProvider, factory, NullLogger<WoolworthsClient>.Instance);

        var results = await client.SearchAsync("Milk");

        Assert.Equal(2, results.Count); // Banner filtered out
        Assert.Equal("Milk 2L", results[0].ProductName);
        Assert.Equal("Woolworths", results[0].StoreName);
        Assert.Equal(3.99m, results[0].Price); // Already in dollars, no /100
        Assert.Equal(2.49m, results[1].Price);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_OnFailure()
    {
        var handler = CreateMockHandler(HttpStatusCode.InternalServerError, "{}");
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://www.woolworths.co.nz") };
        var factory = CreateFactory("Woolworths", httpClient);

        var tokenProvider = new FakeWoolworthsTokenProvider("session-cookie");
        var client = new WoolworthsClient(tokenProvider, factory, NullLogger<WoolworthsClient>.Instance);

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
                        JsonSerializer.Serialize(new { products = new { items = new[] { new { name = "Eggs", type = "Product", price = new { salePrice = 8.99 } } } } }),
                        Encoding.UTF8, "application/json")
                };
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://www.woolworths.co.nz") };
        var factory = CreateFactory("Woolworths", httpClient);

        var tokenProvider = new FakeWoolworthsTokenProvider("cookie");
        var client = new WoolworthsClient(tokenProvider, factory, NullLogger<WoolworthsClient>.Instance);

        CachedTokenProvider.ClearCache();
        var results = await client.SearchAsync("Eggs");

        Assert.Single(results);
        Assert.Equal(8.99m, results[0].Price);
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

public class FakeWoolworthsTokenProvider : WoolworthsTokenProvider
{
    private readonly string _token;

    public FakeWoolworthsTokenProvider(string token)
        : base(CreateScopeFactory(), NullLogger<WoolworthsTokenProvider>.Instance,
            new Mock<IHttpClientFactory>().Object)
    {
        _token = token;
    }

    protected override Task<TokenResponse> FetchTokenAsync(CancellationToken ct)
        => Task.FromResult(new TokenResponse(_token, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30)));

    private static IServiceScopeFactory CreateScopeFactory()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("FakeWoolworthsDb"));
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }
}

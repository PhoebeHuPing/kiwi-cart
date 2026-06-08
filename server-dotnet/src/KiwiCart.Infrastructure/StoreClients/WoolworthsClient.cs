using System.Net;
using System.Text.Json;
using KiwiCart.Core.DTOs;
using KiwiCart.Infrastructure.TokenProviders;
using Microsoft.Extensions.Logging;

namespace KiwiCart.Infrastructure.StoreClients;

public class WoolworthsClient : StoreApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WoolworthsClient(
        WoolworthsTokenProvider tokenProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<WoolworthsClient> logger)
        : base(tokenProvider, logger)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override string StoreName => "Woolworths";

    protected override async Task<IReadOnlyList<PriceResult>?> ExecuteSearchAsync(
        string term, string token, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("Woolworths");
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/v1/products?target=search&search={Uri.EscapeDataString(term)}&inStockProductsOnly=true");
        request.Headers.Add("Cookie", token);
        request.Headers.Add("X-Requested-With", "OnlineShopping.WebApp");

        var response = await client.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            return null;
        }

        using (response)
        {
            response.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("products", out var products)
                || !products.TryGetProperty("items", out var items))
                return [];

            var results = new List<PriceResult>();

            foreach (var p in items.EnumerateArray())
            {
                // Filter non-product items (banners, ads)
                if (p.TryGetProperty("type", out var type) && type.GetString() != "Product")
                    continue;

                var name = p.GetProperty("name").GetString() ?? "";
                var price = p.TryGetProperty("price", out var priceObj)
                    && priceObj.TryGetProperty("salePrice", out var salePrice)
                    ? salePrice.GetDecimal() : 0m;

                results.Add(new PriceResult
                {
                    ProductName = name,
                    StoreName = StoreName,
                    StoreBrand = "Woolworths",
                    Price = price, // Already in dollars
                    RetrievedAt = DateTime.UtcNow
                });
            }

            return results;
        }
    }
}

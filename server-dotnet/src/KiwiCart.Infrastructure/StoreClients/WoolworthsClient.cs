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
    public override string StoreBrand => "Woolworths";

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

                // Skip items without a name property. Woolworths mixes non-product
                // entries (ads, category tiles) into the items array; calling
                // GetProperty("name") on those throws and aborts the whole search.
                if (!p.TryGetProperty("name", out var nameEl))
                    continue;

                var name = nameEl.GetString() ?? "";
                var price = p.TryGetProperty("price", out var priceObj)
                    && priceObj.TryGetProperty("salePrice", out var salePrice)
                    ? salePrice.GetDecimal() : 0m;

                // Extract brand from API
                var brand = p.TryGetProperty("brand", out var br) ? br.GetString() : null;

                // Extract volume and unit price from size object
                string? volume = null;
                string? unitPrice = null;
                if (p.TryGetProperty("size", out var sizeObj))
                {
                    // Try to get volumeSize (e.g., "250mL")
                    if (sizeObj.TryGetProperty("volumeSize", out var vs))
                    {
                        volume = vs.GetString();
                    }

                    // Try to get unit price directly (e.g., "5.2" for "$5.2/1L")
                    if (sizeObj.TryGetProperty("cupPrice", out var cp) && sizeObj.TryGetProperty("cupMeasure", out var cm))
                    {
                        var cupPriceVal = cp.TryGetDecimal(out var cpVal) ? cpVal : 0m;
                        var cupMeasureStr = cm.GetString() ?? "";
                        if (cupPriceVal > 0 && !string.IsNullOrEmpty(cupMeasureStr))
                        {
                            unitPrice = $"${cupPriceVal:F2}/{cupMeasureStr}";
                        }
                    }
                }

                // Normalize product name by prepending brand if needed
                var displayProductName = NormalizeProductName(name, brand);

                // Extract image URL (Woolworths uses images.big / images.small)
                string? imageUrl = null;
                if (p.TryGetProperty("images", out var images))
                {
                    if (images.TryGetProperty("big", out var bigImg))
                        imageUrl = bigImg.GetString()?.Replace("w=200&h=200", "w=400&h=400");
                    else if (images.TryGetProperty("small", out var smallImg))
                        imageUrl = smallImg.GetString();
                }

                results.Add(new PriceResult
                {
                    ProductName = name,
                    DisplayProductName = displayProductName,
                    ImageUrl = imageUrl,
                    StoreName = StoreName,
                    StoreBrand = "Woolworths",
                    Brand = brand,  // Store the actual product brand from API
                    LogoUrl = "/images/woolworths.webp",
                    Address = "Quay St, Auckland CBD",
                    Lat = -36.8475,
                    Lng = 174.767,
                    Price = price, // Already in dollars
                    Volume = volume,
                    UnitPrice = unitPrice,
                    RetrievedAt = DateTime.UtcNow
                });
            }

            return results;
        }
    }
}

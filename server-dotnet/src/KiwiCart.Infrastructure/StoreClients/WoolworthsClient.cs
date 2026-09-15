using System.Net;
using System.Text.Json;
using KiwiCart.Core.DTOs;
using KiwiCart.Infrastructure.TokenProviders;
using Microsoft.Extensions.Logging;

namespace KiwiCart.Infrastructure.StoreClients;

public class WoolworthsClient : StoreApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    // Pagination tuning. Woolworths reports the full match count in
    // products.totalItems (e.g. 421 for "milk") but only returns one page per
    // request. We page through with a large page size and cap the total pages
    // so a broad query cannot fan out into an unbounded number of upstream
    // calls.
    private const int PageSize = 48;
    private const int MaxPages = 5; // up to 240 raw items

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
        string term, string token, CancellationToken ct, string? storeId = null)
    {
        var client = _httpClientFactory.CreateClient("Woolworths");
        var results = new List<PriceResult>();
        int totalItems = int.MaxValue; // updated from the first page's metadata
        int fetched = 0;

        for (int page = 1; page <= MaxPages; page++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"/api/v1/products?target=search&search={Uri.EscapeDataString(term)}" +
                $"&inStockProductsOnly=true&size={PageSize}&page={page}");
            request.Headers.Add("Cookie", token);
            request.Headers.Add("X-Requested-With", "OnlineShopping.WebApp");

            var response = await client.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                // On the first page, signal the base class to refresh the token
                // and retry. On later pages, return what we have so a mid-run
                // expiry does not throw away earlier results.
                if (page == 1)
                    return null;
                break;
            }

            using (response)
            {
                response.EnsureSuccessStatusCode();

                using var doc = await JsonDocument.ParseAsync(
                    await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

                if (!doc.RootElement.TryGetProperty("products", out var products)
                    || !products.TryGetProperty("items", out var items))
                    break;

                // Capture the total match count from the first page.
                if (page == 1 && products.TryGetProperty("totalItems", out var totalEl)
                    && totalEl.TryGetInt32(out var total))
                {
                    totalItems = total;
                }

                int rawInPage = items.GetArrayLength();
                if (rawInPage == 0)
                    break; // no more results

                foreach (var p in items.EnumerateArray())
                {
                    var mapped = MapProduct(p);
                    if (mapped != null)
                        results.Add(mapped);
                }

                fetched += rawInPage;

                // Stop once we've consumed all items the API says exist, or the
                // page came back short (last page).
                if (fetched >= totalItems || rawInPage < PageSize)
                    break;
            }
        }

        return results;
    }

    /// <summary>
    /// Map a single Woolworths product JSON element to a PriceResult. Returns
    /// null for non-product entries (ads, banners, category tiles) and for
    /// entries missing a name — Woolworths mixes these into the items array.
    /// </summary>
    private static PriceResult? MapProduct(JsonElement p)
    {
        // Filter non-product items (banners, ads).
        if (p.TryGetProperty("type", out var type) && type.GetString() != "Product")
            return null;

        // Skip items without a name property. Calling GetProperty("name") on
        // those would throw and abort the whole page.
        if (!p.TryGetProperty("name", out var nameEl))
            return null;

        var name = nameEl.GetString() ?? "";
        var price = p.TryGetProperty("price", out var priceObj)
            && priceObj.TryGetProperty("salePrice", out var salePrice)
            ? salePrice.GetDecimal() : 0m;

        // Woolworths normally returns brand as a string, but some responses
        // return an object such as { "name": "Lewis Road Creamery" }.
        var brand = ExtractBrand(p);

        // Extract Woolworths stable ids: sku (product id) and barcode (GTIN).
        var sku = p.TryGetProperty("sku", out var skuEl) ? skuEl.GetString() : null;
        var barcode = p.TryGetProperty("barcode", out var bcEl) ? bcEl.GetString() : null;

        // Extract volume and unit price from the size object.
        string? volume = null;
        string? unitPrice = null;
        if (p.TryGetProperty("size", out var sizeObj))
        {
            if (sizeObj.TryGetProperty("volumeSize", out var vs))
                volume = vs.GetString();

            if (sizeObj.TryGetProperty("cupPrice", out var cp) && sizeObj.TryGetProperty("cupMeasure", out var cm))
            {
                var cupPriceVal = cp.TryGetDecimal(out var cpVal) ? cpVal : 0m;
                var cupMeasureStr = cm.GetString() ?? "";
                if (cupPriceVal > 0 && !string.IsNullOrEmpty(cupMeasureStr))
                    unitPrice = $"${cupPriceVal:F2}/{cupMeasureStr}";
            }

        }

        // Normalize product name by prepending brand if needed.
        var displayProductName = NormalizeProductName(name, brand);

        // Extract image URL (Woolworths uses images.big / images.small).
        string? imageUrl = null;
        if (p.TryGetProperty("images", out var images))
        {
            if (images.TryGetProperty("big", out var bigImg))
                imageUrl = bigImg.GetString()?.Replace("w=200&h=200", "w=400&h=400");
            else if (images.TryGetProperty("small", out var smallImg))
                imageUrl = smallImg.GetString();
        }

        return new PriceResult
        {
            ProductName = name,
            DisplayProductName = displayProductName,
            ImageUrl = imageUrl,
            StoreName = "Woolworths",
            StoreBrand = "Woolworths",
            Brand = brand,
            ProductId = sku,   // Woolworths stable product id
            Gtin = barcode,    // Woolworths barcode (GTIN), used to fill product_gtins
            LogoUrl = "/images/woolworths.webp",
            Address = "Quay St, Auckland CBD",
            Lat = -36.8475,
            Lng = 174.767,
            Price = price, // Already in dollars
            Volume = volume,
            UnitPrice = unitPrice,
            RetrievedAt = DateTime.UtcNow
        };
    }

    private static string? ExtractBrand(JsonElement product)
    {
        if (!product.TryGetProperty("brand", out var brand))
            return null;

        if (brand.ValueKind == JsonValueKind.String)
            return brand.GetString()?.Trim();

        if (brand.ValueKind == JsonValueKind.Object
            && brand.TryGetProperty("name", out var name)
            && name.ValueKind == JsonValueKind.String)
            return name.GetString()?.Trim();

        return null;
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.TokenProviders;
using Microsoft.Extensions.Logging;

namespace KiwiCart.Infrastructure.StoreClients;

public class NewWorldClient : StoreApiClient, IGtinLookupClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const string StoreId = "dbdfdd2a-55f7-4870-9b51-979286323647";
    private const int PageSize = 50;
    private const int MaxPages = 2;

    public NewWorldClient(
        NewWorldTokenProvider tokenProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<NewWorldClient> logger)
        : base(tokenProvider, logger)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override string StoreName => "New World";
    public override string StoreBrand => "NewWorld";

    protected override async Task<IReadOnlyList<PriceResult>?> ExecuteSearchAsync(
        string term, string token, CancellationToken ct, string? storeId = null)
    {
        var client = _httpClientFactory.CreateClient("NewWorld");
        // Use the caller-provided store (nearest to the user) when available,
        // otherwise fall back to the default store.
        var effectiveStoreId = string.IsNullOrEmpty(storeId) ? StoreId : storeId;
        var results = new List<PriceResult>();
        for (var page = 0; page < MaxPages; page++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                "/v1/edge/search/paginated/products");
            request.Headers.Authorization = new("Bearer", token);
            request.Content = JsonContent.Create(new
            {
                algoliaQuery = new { query = term },
                storeId = effectiveStoreId,
                hitsPerPage = PageSize,
                page,
                sortOrder = "NI_POPULARITY_ASC"
            });

            var response = await client.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                return page == 0 ? null : results;
            }

            using (response)
            {
                response.EnsureSuccessStatusCode();

                using var doc = await JsonDocument.ParseAsync(
                    await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

                if (!doc.RootElement.TryGetProperty("products", out var products))
                    break;

                var rawInPage = products.GetArrayLength();
                foreach (var p in products.EnumerateArray())
                {
                var name = p.GetProperty("name").GetString() ?? "";
                var priceInCents = p.TryGetProperty("singlePrice", out var sp)
                    && sp.TryGetProperty("price", out var priceEl)
                    ? priceEl.GetDecimal() : 0m;
                var price = priceInCents / 100m;

                // Extract productId, displayName (volume), and brand
                var productId = p.TryGetProperty("productId", out var pid) ? pid.GetString() : null;
                var displayName = p.TryGetProperty("displayName", out var dn) ? dn.GetString() : null;
                var brand = p.TryGetProperty("brand", out var br) ? br.GetString() : null;

                // Normalize product name by prepending brand if needed
                var displayProductName = NormalizeProductName(name, brand);

                // Extract unit price from singlePrice.comparativePrice object
                string? unitPrice = null;
                if (p.TryGetProperty("singlePrice", out var singlePrice)
                    && singlePrice.TryGetProperty("comparativePrice", out var compPrice))
                {
                    // comparativePrice is an object with pricePerUnit, unitQuantityUom, measureDescription
                    if (compPrice.ValueKind == System.Text.Json.JsonValueKind.Object
                        && compPrice.TryGetProperty("pricePerUnit", out var ppu)
                        && compPrice.TryGetProperty("measureDescription", out var md))
                    {
                        var ppuInCents = ppu.GetDecimal();
                        var ppuInDollars = ppuInCents / 100m;
                        var measure = md.GetString() ?? "1L";
                        unitPrice = $"${ppuInDollars:F2}/{measure}";
                    }
                }

                // Extract image URL from API response (fallback to fsimg CDN)
                var simpleId = productId?.Split('-')[0] ?? "";
                string? imageUrl = null;
                if (p.TryGetProperty("images", out var images)
                    && images.TryGetProperty("primaryImages", out var primary)
                    && primary.TryGetProperty("400px", out var img400))
                {
                    imageUrl = img400.GetString();
                }
                imageUrl ??= string.IsNullOrEmpty(simpleId)
                    ? null
                    : $"https://a.fsimg.co.nz/product/retail/fan/image/400x400/{simpleId}.png";

                results.Add(new PriceResult
                {
                    ProductName = name,
                    DisplayProductName = displayProductName,
                    ImageUrl = imageUrl,
                    StoreName = StoreName,
                    StoreBrand = "NewWorld",
                    Brand = brand, // Store the actual product brand
                    LogoUrl = "/images/new-world.webp",
                    Address = "Victoria Park, Auckland",
                    Lat = -36.8485,
                    Lng = 174.7523,
                    Price = price,
                    ProductId = productId,
                    Volume = displayName,
                    UnitPrice = unitPrice,
                    RetrievedAt = DateTime.UtcNow
                });
                }

                if (rawInPage < PageSize)
                    break;
            }
        }

        return results;
    }

    /// <summary>
    /// Resolve a product's GTIN via the Foodstuffs detail endpoint (the "sku"
    /// field). Returns null on any failure.
    /// </summary>
    public async Task<string?> FetchGtinByProductIdAsync(string productId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(productId))
            return null;

        try
        {
            var token = await GetTokenAsync(ct);
            var client = _httpClientFactory.CreateClient("NewWorld");
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"/v1/edge/store/{StoreId}/product/{Uri.EscapeDataString(productId)}");
            request.Headers.Authorization = new("Bearer", token);

            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return doc.RootElement.TryGetProperty("sku", out var sku) ? sku.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Fetch all New World stores from the store-list endpoint
    /// (GET /v1/edge/store). Uses the same bearer token as search. Returns an
    /// empty list on failure. Same Foodstuffs API shape as Pak'nSave.
    /// </summary>
    public async Task<IReadOnlyList<StoreInfo>> FetchStoresAsync(CancellationToken ct = default)
    {
        try
        {
            var token = await GetTokenAsync(ct);
            var client = _httpClientFactory.CreateClient("NewWorld");
            using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/edge/store");
            request.Headers.Authorization = new("Bearer", token);

            using var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("stores", out var stores))
                return [];

            var results = new List<StoreInfo>();
            foreach (var s in stores.EnumerateArray())
            {
                if (!s.TryGetProperty("id", out var idEl)) continue;
                var id = idEl.GetString();
                if (string.IsNullOrEmpty(id)) continue;

                var name = s.TryGetProperty("name", out var nEl) ? nEl.GetString() ?? "" : "";
                var address = s.TryGetProperty("address", out var aEl) ? aEl.GetString() ?? "" : "";
                var lat = s.TryGetProperty("latitude", out var latEl) && latEl.TryGetDouble(out var latVal) ? latVal : 0;
                var lng = s.TryGetProperty("longitude", out var lngEl) && lngEl.TryGetDouble(out var lngVal) ? lngVal : 0;

                results.Add(new StoreInfo(id, name, "NewWorld", lat, lng, address));
            }
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "New World: failed to fetch store list");
            return [];
        }
    }
}

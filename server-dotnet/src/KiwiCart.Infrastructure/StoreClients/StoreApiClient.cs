using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.TokenProviders;
using Microsoft.Extensions.Logging;

namespace KiwiCart.Infrastructure.StoreClients;

public abstract class StoreApiClient
{
    private readonly CachedTokenProvider _tokenProvider;
    protected readonly ILogger _logger;

    protected StoreApiClient(CachedTokenProvider tokenProvider, ILogger logger)
    {
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public abstract string StoreName { get; }

    /// <summary>Stable brand identifier used as the cache/store key (e.g. PakNSave, NewWorld, Woolworths).</summary>
    public abstract string StoreBrand { get; }

    public async Task<IReadOnlyList<PriceResult>> SearchAsync(
        string term, CancellationToken ct = default, string? storeId = null)
    {
        try
        {
            var token = await _tokenProvider.GetTokenAsync(ct);
            var results = await ExecuteSearchAsync(term, token, ct, storeId);

            // Retry once on 401 with fresh token
            if (results is null)
            {
                _logger.LogWarning("{Store}: Token expired, refreshing...", StoreName);
                await _tokenProvider.InvalidateTokenAsync(_tokenProvider.StoreName, ct);
                token = await _tokenProvider.GetTokenAsync(ct);
                results = await ExecuteSearchAsync(term, token, ct, storeId);
            }

            return results ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Store}: Search failed for '{Term}'", StoreName, term);
            return [];
        }
    }

    /// <summary>
    /// Execute search. Return null to signal 401 (token expired) for retry.
    /// <paramref name="storeId"/> optionally overrides the store to query
    /// (used for location-based store selection); clients that do not support
    /// per-store pricing ignore it.
    /// </summary>
    protected abstract Task<IReadOnlyList<PriceResult>?> ExecuteSearchAsync(
        string term, string token, CancellationToken ct, string? storeId = null);

    /// <summary>
    /// Obtain a store token for use by derived clients on non-search endpoints
    /// (e.g. product detail lookups). Uses the same cached token provider.
    /// </summary>
    protected Task<string> GetTokenAsync(CancellationToken ct) => _tokenProvider.GetTokenAsync(ct);

    /// <summary>
    /// Normalize product name by prepending brand if not already present.
    /// Example: "Calci-Yum Milk..." with brand "Anchor" → "Anchor Calci-Yum Milk..."
    /// </summary>
    protected static string NormalizeProductName(string productName, string? brand)
    {
        if (string.IsNullOrEmpty(brand) || string.IsNullOrEmpty(productName))
            return productName;

        // Check if product name already contains brand (case-insensitive)
        if (productName.Contains(brand, StringComparison.OrdinalIgnoreCase))
            return productName;

        // Prepend brand to product name
        return $"{brand} {productName}".Trim();
    }
}

using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.TokenProviders;
using Microsoft.Extensions.Logging;

namespace KiwiCart.Infrastructure.StoreClients;

public abstract class StoreApiClient
{
    private readonly CachedTokenProvider _tokenProvider;
    private readonly ILogger _logger;

    protected StoreApiClient(CachedTokenProvider tokenProvider, ILogger logger)
    {
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public abstract string StoreName { get; }

    public async Task<IReadOnlyList<PriceResult>> SearchAsync(string term, CancellationToken ct = default)
    {
        try
        {
            var token = await _tokenProvider.GetTokenAsync(ct);
            var results = await ExecuteSearchAsync(term, token, ct);

            // Retry once on 401 with fresh token
            if (results is null)
            {
                _logger.LogWarning("{Store}: Token expired, refreshing...", StoreName);
                await _tokenProvider.InvalidateTokenAsync(_tokenProvider.StoreName, ct);
                token = await _tokenProvider.GetTokenAsync(ct);
                results = await ExecuteSearchAsync(term, token, ct);
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
    /// </summary>
    protected abstract Task<IReadOnlyList<PriceResult>?> ExecuteSearchAsync(
        string term, string token, CancellationToken ct);
}

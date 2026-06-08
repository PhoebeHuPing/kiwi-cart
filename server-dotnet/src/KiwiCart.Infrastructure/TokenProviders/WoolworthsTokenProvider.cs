using System.Net.Http.Json;
using KiwiCart.Core.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KiwiCart.Infrastructure.TokenProviders;

/// <summary>
/// Woolworths uses a session cookie rather than a Bearer token.
/// We store the session cookie value as the "token" in our cache system.
/// </summary>
public class WoolworthsTokenProvider : CachedTokenProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WoolworthsTokenProvider(
        IServiceScopeFactory scopeFactory,
        ILogger<WoolworthsTokenProvider> logger,
        IHttpClientFactory httpClientFactory)
        : base(scopeFactory, logger)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override string StoreName => "Woolworths";

    protected override async Task<TokenResponse> FetchTokenAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("WoolworthsAuth");
        using var response = await client.PostAsJsonAsync("/api/v1/session", new { }, ct);
        response.EnsureSuccessStatusCode();

        // Extract session cookie
        var cookie = string.Empty;
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            cookie = string.Join("; ", cookies.Select(c => c.Split(';')[0]));
        }

        if (string.IsNullOrEmpty(cookie))
            throw new InvalidOperationException("No session cookie returned from Woolworths");

        // Session cookies typically last 30 minutes
        return new TokenResponse(cookie, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30));
    }
}

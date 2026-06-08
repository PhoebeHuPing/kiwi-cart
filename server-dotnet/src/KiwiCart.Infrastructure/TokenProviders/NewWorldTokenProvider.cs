using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KiwiCart.Core.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KiwiCart.Infrastructure.TokenProviders;

public class NewWorldTokenProvider : CachedTokenProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public NewWorldTokenProvider(
        IServiceScopeFactory scopeFactory,
        ILogger<NewWorldTokenProvider> logger,
        IHttpClientFactory httpClientFactory)
        : base(scopeFactory, logger)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override string StoreName => "NewWorld";

    protected override async Task<TokenResponse> FetchTokenAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("NewWorldAuth");
        using var response = await client.PostAsJsonAsync(
            "/api/user/get-current-user", new { }, ct);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var token = doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("No access_token in New World response");

        var expiresAt = ParseJwtExpiry(token) ?? DateTime.UtcNow.AddHours(1);
        return new TokenResponse(token, DateTime.UtcNow, expiresAt);
    }

    private static DateTime? ParseJwtExpiry(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) return null;
        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("exp", out var exp))
                return DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()).UtcDateTime;
        }
        catch { }
        return null;
    }
}

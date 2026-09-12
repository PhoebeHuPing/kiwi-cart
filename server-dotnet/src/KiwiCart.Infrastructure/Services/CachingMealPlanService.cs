using System.Security.Cryptography;
using System.Text;
using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KiwiCart.Infrastructure.Services;

/// <summary>
/// Caching decorator over an inner <see cref="IMealPlanService"/>. Assembling a
/// meal plan costs one billed Gemini call plus an N-ingredient price fan-out,
/// so identical prompts within the TTL window are served from an in-process
/// cache instead of recomputing.
///
/// This is a decorator (not baked into <see cref="MealPlanService"/>) so the
/// planning logic stays single-responsibility and independently testable, in
/// the same spirit as the project's other independent cache layers.
/// </summary>
public class CachingMealPlanService : IMealPlanService
{
    private readonly IMealPlanService _inner;
    private readonly IMemoryCache _cache;
    private readonly MealPlanCacheOptions _options;
    private readonly ILogger<CachingMealPlanService> _logger;

    public CachingMealPlanService(
        IMealPlanService inner,
        IMemoryCache cache,
        IOptions<MealPlanCacheOptions> options,
        ILogger<CachingMealPlanService> logger)
    {
        _inner = inner;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MealPlanResponse> PlanAsync(string prompt, CancellationToken ct = default)
    {
        // A non-positive TTL (or the master switch off) disables caching. The
        // memory cache rejects a zero/negative expiry, so treat it as bypass
        // rather than letting it throw.
        if (!_options.Enabled || _options.TtlMinutes <= 0)
            return await _inner.PlanAsync(prompt, ct);

        var key = BuildKey(prompt);

        if (_cache.TryGetValue(key, out MealPlanResponse? cached) && cached is not null)
        {
            _logger.LogDebug("Meal-plan cache hit.");
            return cached;
        }

        var response = await _inner.PlanAsync(prompt, ct);

        // Only cache responses that produced at least one ingredient. Empty
        // results are usually a transient parse/AI miss and are cheap to retry,
        // so we avoid pinning a bad result for the whole TTL.
        if (response.Items.Count > 0)
        {
            var entryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.TtlMinutes),
                Size = 1,
            };
            _cache.Set(key, response, entryOptions);
            _logger.LogDebug("Meal-plan cached for {TtlMinutes} minute(s).", _options.TtlMinutes);
        }

        return response;
    }

    /// <summary>
    /// Build a stable cache key from a normalized prompt. The prompt is
    /// free-text, so it is trimmed and lower-cased (invariant) then hashed to
    /// keep the key bounded and avoid storing raw user input as a key.
    /// </summary>
    private static string BuildKey(string prompt)
    {
        var normalized = prompt.Trim().ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return "mealplan:" + Convert.ToHexString(bytes);
    }
}

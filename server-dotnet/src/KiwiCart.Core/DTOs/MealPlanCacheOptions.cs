namespace KiwiCart.Core.DTOs;

/// <summary>
/// Configuration for caching assembled meal-plan responses, bound from the
/// "MealPlanCache" section of appsettings. Caching the whole assembled
/// response lets us skip both the (billed, slow) Gemini call and the per
/// ingredient price fan-out for repeated prompts. The TTL is deliberately
/// shorter than the underlying 24h price cache because a costed plan goes
/// stale faster than a single price point.
/// </summary>
public class MealPlanCacheOptions
{
    public const string SectionName = "MealPlanCache";

    /// <summary>Master switch; disable to bypass caching entirely.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How long an assembled meal-plan response stays cached.</summary>
    public int TtlMinutes { get; set; } = 60;

    /// <summary>
    /// Upper bound on distinct cached prompts, to keep memory bounded on a
    /// free-text (unbounded) key space.
    /// </summary>
    public int MaxEntries { get; set; } = 500;
}

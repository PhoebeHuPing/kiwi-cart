using System.Text.Json.Serialization;

namespace KiwiCart.Core.DTOs;

/// <summary>
/// Personalized shopping recommendations for a signed-in user. The AI proposes
/// items to consider (based on the user's favorites), and each proposal is
/// priced across supermarkets via the existing price comparison service so the
/// AI never sees or invents real prices.
/// </summary>
public class SuggestionsResponse
{
    [JsonPropertyName("items")]
    public IReadOnlyList<SuggestionItem> Items { get; set; } = [];

    /// <summary>
    /// Total potential saving across all suggestions that had a computable
    /// cross-store saving (cheapest vs. next-cheapest store). Zero when no
    /// suggestion had at least two priced stores.
    /// </summary>
    [JsonPropertyName("total_potential_saving")]
    public decimal TotalPotentialSaving { get; set; }
}

/// <summary>
/// A single personalized suggestion: the product to consider, a short
/// human-readable reason from the AI, the cheapest matching product (null when
/// no match was found), and the potential saving from buying at the cheapest
/// store rather than the next-cheapest one.
/// </summary>
public class SuggestionItem
{
    [JsonPropertyName("product")]
    public string Product { get; set; } = string.Empty;

    /// <summary>Short rationale from the AI (e.g. "A staple you buy often").</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("cheapest")]
    public PriceResult? Cheapest { get; set; }

    /// <summary>
    /// Saving from buying at the cheapest store versus the next-cheapest store
    /// for this product, based on currently cached/live prices. Null when fewer
    /// than two stores returned a price (no comparison possible).
    /// </summary>
    [JsonPropertyName("potential_saving")]
    public decimal? PotentialSaving { get; set; }
}

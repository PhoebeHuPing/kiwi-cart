using System.Text.Json.Serialization;

namespace KiwiCart.Core.DTOs;

/// <summary>
/// Result of an AI meal plan: the ingredients the model extracted from the
/// user's prompt, each paired with the cheapest matching product found across
/// supermarkets (via the existing price comparison service).
/// </summary>
public class MealPlanResponse
{
    [JsonPropertyName("items")]
    public IReadOnlyList<MealPlanItem> Items { get; set; } = [];

    /// <summary>
    /// Sum of the cheapest price for each ingredient that had at least one
    /// match. Ingredients with no match are excluded from the total.
    /// </summary>
    [JsonPropertyName("estimated_total")]
    public decimal EstimatedTotal { get; set; }
}

/// <summary>
/// One extracted ingredient and its cheapest matching product (null when no
/// product was found for that ingredient).
/// </summary>
public class MealPlanItem
{
    [JsonPropertyName("ingredient")]
    public string Ingredient { get; set; } = string.Empty;

    [JsonPropertyName("cheapest")]
    public PriceResult? Cheapest { get; set; }
}

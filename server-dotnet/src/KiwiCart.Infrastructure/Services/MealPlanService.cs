using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KiwiCart.Infrastructure.Services;

public class MealPlanService : IMealPlanService
{
    // Bound the number of ingredients we price. Each ingredient fans out to a
    // price comparison (which itself hits multiple stores), so this caps both
    // latency and external-call volume from a single AI prompt.
    private const int MaxIngredients = 15;

    private readonly IGeminiClient _gemini;
    private readonly IPriceComparisonService _priceComparison;
    private readonly ILogger<MealPlanService> _logger;

    public MealPlanService(
        IGeminiClient gemini,
        IPriceComparisonService priceComparison,
        ILogger<MealPlanService> logger)
    {
        _gemini = gemini;
        _priceComparison = priceComparison;
        _logger = logger;
    }

    public async Task<MealPlanResponse> PlanAsync(string prompt, CancellationToken ct = default)
    {
        // Step 1: AI extracts a plain list of ingredient names. The AI only
        // parses intent; it is never given or asked about real prices.
        var raw = await _gemini.GenerateContentAsync(BuildIngredientPrompt(prompt), ct);
        var ingredients = ParseIngredients(raw);

        if (ingredients.Count == 0)
        {
            _logger.LogInformation("Meal plan produced no ingredients for prompt.");
            return new MealPlanResponse { Items = [], EstimatedTotal = 0m };
        }

        // Step 2: price each ingredient in parallel by reusing the existing
        // comparison service (cache, aggregation and degradation all apply).
        var priced = await Task.WhenAll(ingredients.Select(async ingredient =>
        {
            var results = await _priceComparison.CompareAsync(ingredient, ct);
            // CompareAsync returns results ordered by ascending price, so the
            // first entry is the cheapest match (or none).
            return new MealPlanItem
            {
                Ingredient = ingredient,
                Cheapest = results.Count > 0 ? results[0] : null
            };
        }));

        var items = priced.ToList();
        var estimatedTotal = items
            .Where(i => i.Cheapest is not null)
            .Sum(i => i.Cheapest!.Price);

        return new MealPlanResponse
        {
            Items = items,
            EstimatedTotal = estimatedTotal
        };
    }

    private static string BuildIngredientPrompt(string userPrompt) =>
        "You are a grocery shopping assistant for New Zealand supermarkets. " +
        "From the user's request below, list the individual grocery ingredients " +
        "needed. Reply with ONLY the ingredient names, one per line, with no " +
        "numbering, quantities, brands, or extra commentary. Use simple, " +
        "searchable product names (e.g. \"chicken breast\", \"onion\", \"rice\").\n\n" +
        "User request: " + userPrompt;

    /// <summary>
    /// Parse the model's plain-text output into a clean, de-duplicated,
    /// length-capped list of ingredient names. Tolerant of newline- or
    /// comma-separated output and stray list markers.
    /// </summary>
    private static List<string> ParseIngredients(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        var tokens = raw.Split(
            new[] { '\n', '\r', ',' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var token in tokens)
        {
            // Strip common leading list markers ("-", "*", "1.", "1)").
            var cleaned = token.TrimStart('-', '*', '•', ' ', '\t');
            var dotIdx = cleaned.IndexOf('.');
            if (dotIdx is > 0 and <= 3 && cleaned[..dotIdx].All(char.IsDigit))
                cleaned = cleaned[(dotIdx + 1)..].Trim();
            var parenIdx = cleaned.IndexOf(')');
            if (parenIdx is > 0 and <= 3 && cleaned[..parenIdx].All(char.IsDigit))
                cleaned = cleaned[(parenIdx + 1)..].Trim();

            cleaned = cleaned.Trim();
            if (cleaned.Length == 0)
                continue;

            if (seen.Add(cleaned))
            {
                result.Add(cleaned);
                if (result.Count >= MaxIngredients)
                    break;
            }
        }

        return result;
    }
}

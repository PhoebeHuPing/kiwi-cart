using System.Text;
using System.Text.Json;
using KiwiCart.Core.DTOs;
using KiwiCart.Core.Exceptions;
using KiwiCart.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KiwiCart.Infrastructure.Services;

/// <summary>
/// Builds personalized suggestions from a user's favorites. The AI proposes
/// product names plus a short reason; each proposal is then priced via the
/// existing <see cref="IPriceComparisonService"/> (cache, aggregation and
/// degradation all apply). The AI is only given the user's favorite names to
/// reason about intent — it never sees or produces real prices.
/// </summary>
public class SuggestionService : ISuggestionService
{
    // Bound the number of suggestions we price. Each suggestion fans out to a
    // price comparison (which itself hits multiple stores), so this caps both
    // latency and external-call volume.
    private const int MaxSuggestions = 6;

    private readonly IFavoritesService _favorites;
    private readonly IGeminiClient _gemini;
    private readonly IPriceComparisonService _priceComparison;
    private readonly ILogger<SuggestionService> _logger;

    public SuggestionService(
        IFavoritesService favorites,
        IGeminiClient gemini,
        IPriceComparisonService priceComparison,
        ILogger<SuggestionService> logger)
    {
        _favorites = favorites;
        _gemini = gemini;
        _priceComparison = priceComparison;
        _logger = logger;
    }

    public async Task<SuggestionsResponse> GetSuggestionsAsync(string userId, CancellationToken ct = default)
    {
        var favorites = await _favorites.GetFavoritesAsync(userId, ct);
        if (favorites.Count == 0)
        {
            // No history to personalize from; return empty rather than
            // inventing generic suggestions.
            _logger.LogInformation("No favorites for user; returning no suggestions.");
            return new SuggestionsResponse { Items = [], TotalPotentialSaving = 0m };
        }

        // Step 1: AI proposes product names + reasons as JSON. It only sees the
        // favorite names (intent), never prices. If the AI call fails (e.g. the
        // Gemini free-tier quota is exhausted and returns 429), degrade
        // gracefully to no suggestions rather than surfacing a 5xx error — the
        // rest of My Kitchen must still render.
        string raw;
        try
        {
            raw = await _gemini.GenerateContentAsync(BuildPrompt(favorites), ct);
        }
        catch (GeminiApiException ex)
        {
            _logger.LogWarning(ex, "Gemini call failed; returning no suggestions.");
            return new SuggestionsResponse { Items = [], TotalPotentialSaving = 0m };
        }

        var proposals = ParseProposals(raw, favorites);

        if (proposals.Count == 0)
        {
            _logger.LogInformation("AI produced no usable suggestions.");
            return new SuggestionsResponse { Items = [], TotalPotentialSaving = 0m };
        }

        // Step 2: price each proposal in parallel, reusing the comparison
        // service. Compute the cross-store saving from the returned results.
        var items = await Task.WhenAll(proposals.Select(async proposal =>
        {
            var results = await _priceComparison.CompareAsync(proposal.Product, ct);
            // Results are ordered by ascending price, so [0] is the cheapest.
            var cheapest = results.Count > 0 ? results[0] : null;
            var saving = ComputeSaving(results);

            return new SuggestionItem
            {
                Product = proposal.Product,
                Reason = proposal.Reason,
                Cheapest = cheapest,
                PotentialSaving = saving,
            };
        }));

        var list = items.ToList();
        var totalSaving = list
            .Where(i => i.PotentialSaving is not null)
            .Sum(i => i.PotentialSaving!.Value);

        return new SuggestionsResponse
        {
            Items = list,
            TotalPotentialSaving = totalSaving,
        };
    }

    /// <summary>
    /// Potential saving for a product = price at the next-cheapest store minus
    /// the cheapest store, using currently cached/live prices. Null when fewer
    /// than two stores returned a price (no comparison possible).
    ///
    /// Note: the price store keeps only the latest price per (product, store),
    /// so this is a cross-store saving at a point in time, not a historical
    /// trend. True over-time price movement would need a price-history schema
    /// (out of scope for this phase).
    /// </summary>
    private static decimal? ComputeSaving(IReadOnlyList<PriceResult> results)
    {
        if (results.Count < 2)
            return null;

        // results are ascending by price; distinct stores only.
        var distinctByStore = results
            .GroupBy(r => r.StoreName)
            .Select(g => g.Min(r => r.Price))
            .OrderBy(p => p)
            .ToList();

        if (distinctByStore.Count < 2)
            return null;

        var saving = distinctByStore[1] - distinctByStore[0];
        return saving > 0 ? saving : null;
    }

    private static string BuildPrompt(IReadOnlyList<string> favorites) =>
        "You are a grocery shopping assistant for New Zealand supermarkets. " +
        "A shopper has favorited these items:\n" +
        string.Join("\n", favorites.Select(f => "- " + f)) + "\n\n" +
        "Suggest up to " + MaxSuggestions + " grocery products this shopper is " +
        "likely to want to buy — a mix of their recurring staples and sensible " +
        "complementary items. Use simple, searchable product names (e.g. " +
        "\"chicken breast\", \"onion\", \"rice\"), no brands or quantities.\n\n" +
        "Reply with ONLY a JSON array, no markdown, where each element is an " +
        "object with \"product\" (string) and \"reason\" (a short shopper-" +
        "facing sentence, max 12 words). Example:\n" +
        "[{\"product\":\"milk\",\"reason\":\"A staple you buy regularly\"}]";

    private readonly record struct Proposal(string Product, string Reason);

    /// <summary>
    /// Parse the model's JSON array of {product, reason}. Tolerant of markdown
    /// code fences and stray prose around the array. De-duplicates by product
    /// (case-insensitive) and caps the count.
    /// </summary>
    private List<Proposal> ParseProposals(string raw, IReadOnlyList<string> favorites)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        var json = ExtractJsonArray(raw);
        if (json is null)
        {
            _logger.LogWarning("AI suggestion response did not contain a JSON array.");
            return [];
        }

        List<Proposal> parsed;
        try
        {
            parsed = ParseJsonProposals(json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI suggestion JSON.");
            return [];
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<Proposal>();
        foreach (var p in parsed)
        {
            var product = p.Product.Trim();
            if (product.Length == 0)
                continue;
            if (!seen.Add(product))
                continue;

            var reason = string.IsNullOrWhiteSpace(p.Reason)
                ? "Suggested for you"
                : p.Reason.Trim();

            result.Add(new Proposal(product, reason));
            if (result.Count >= MaxSuggestions)
                break;
        }

        return result;
    }

    private static List<Proposal> ParseJsonProposals(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var result = new List<Proposal>();

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var el in doc.RootElement.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.Object)
                continue;

            var product = el.TryGetProperty("product", out var pEl)
                && pEl.ValueKind == JsonValueKind.String
                ? pEl.GetString() ?? ""
                : "";

            var reason = el.TryGetProperty("reason", out var rEl)
                && rEl.ValueKind == JsonValueKind.String
                ? rEl.GetString() ?? ""
                : "";

            if (product.Length > 0)
                result.Add(new Proposal(product, reason));
        }

        return result;
    }

    /// <summary>
    /// Extract the first top-level JSON array substring from the model output,
    /// tolerating markdown fences or surrounding prose.
    /// </summary>
    private static string? ExtractJsonArray(string raw)
    {
        var start = raw.IndexOf('[');
        var end = raw.LastIndexOf(']');
        if (start < 0 || end < 0 || end <= start)
            return null;
        return raw.Substring(start, end - start + 1);
    }
}

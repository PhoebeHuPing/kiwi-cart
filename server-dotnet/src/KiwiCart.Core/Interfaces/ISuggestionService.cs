using KiwiCart.Core.DTOs;

namespace KiwiCart.Core.Interfaces;

/// <summary>
/// Produces personalized shopping suggestions for a user: aggregates their
/// favorites, asks the AI client to propose relevant items (staples they buy
/// often and sensible complements), then prices each proposal across
/// supermarkets and computes any cross-store saving.
/// </summary>
public interface ISuggestionService
{
    Task<SuggestionsResponse> GetSuggestionsAsync(string userId, CancellationToken ct = default);
}

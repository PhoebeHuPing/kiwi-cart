using KiwiCart.Core.DTOs;

namespace KiwiCart.Core.Interfaces;

/// <summary>
/// Turns a plain-language meal/shopping prompt into a costed shopping list:
/// extracts ingredients via the AI client, then prices each ingredient using
/// the existing price comparison service.
/// </summary>
public interface IMealPlanService
{
    Task<MealPlanResponse> PlanAsync(string prompt, CancellationToken ct = default);
}

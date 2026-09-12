namespace KiwiCart.Core.DTOs;

/// <summary>
/// Request for the AI meal-plan endpoint. The user describes what they want to
/// cook or shop for in plain language (e.g. "ingredients for a chicken curry
/// for 4 people").
/// </summary>
public class MealPlanRequest
{
    public string Prompt { get; set; } = string.Empty;
}

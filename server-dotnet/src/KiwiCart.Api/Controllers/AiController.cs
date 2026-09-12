using System.Security.Claims;
using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KiwiCart.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AiController : ControllerBase
{
    // Guard against oversized prompts before spending an AI call.
    private const int MaxPromptLength = 500;

    private readonly IMealPlanService _mealPlanService;
    private readonly ISuggestionService _suggestionService;

    public AiController(
        IMealPlanService mealPlanService,
        ISuggestionService suggestionService)
    {
        _mealPlanService = mealPlanService;
        _suggestionService = suggestionService;
    }

    /// <summary>
    /// Turn a plain-language meal/shopping request into a costed shopping list:
    /// AI extracts the ingredients, then each is priced across supermarkets.
    /// </summary>
    [HttpPost("meal-plan")]
    [EnableRateLimiting("ai")]
    [ProducesResponseType(typeof(MealPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<MealPlanResponse>> MealPlan(
        [FromBody] MealPlanRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return Problem("Prompt is required.", statusCode: 400);

        if (request.Prompt.Length > MaxPromptLength)
            return Problem($"Prompt must be {MaxPromptLength} characters or fewer.", statusCode: 400);

        var result = await _mealPlanService.PlanAsync(request.Prompt.Trim(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Personalized shopping suggestions for the signed-in user: the AI
    /// proposes items based on the user's favorites, then each is priced across
    /// supermarkets with any cross-store saving computed.
    /// </summary>
    [Authorize]
    [HttpGet("suggestions")]
    [EnableRateLimiting("ai")]
    [ProducesResponseType(typeof(SuggestionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<SuggestionsResponse>> Suggestions(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var result = await _suggestionService.GetSuggestionsAsync(userId, ct);
        return Ok(result);
    }
}

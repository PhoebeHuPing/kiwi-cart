using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KiwiCart.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
        => _feedbackService = feedbackService;

    /// <summary>Public: list all feedback, newest first. Does not expose user_id.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FeedbackResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FeedbackResponse>>> GetAll(CancellationToken ct)
        => Ok(await _feedbackService.GetAllAsync(ct));

    /// <summary>Authenticated: submit a feedback message.</summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(FeedbackResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FeedbackResponse>> Submit(
        [FromBody] FeedbackRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return Problem("Message is required.", statusCode: 400);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var userName = User.FindFirstValue("name");

        var created = await _feedbackService.CreateAsync(userId, userName, request, ct);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }
}

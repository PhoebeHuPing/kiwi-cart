using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace KiwiCart.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IPriceComparisonService _priceComparison;

    public ProductsController(IPriceComparisonService priceComparison)
    {
        _priceComparison = priceComparison;
    }

    [HttpGet("compare")]
    [OutputCache(Duration = 300)]
    [ProducesResponseType(typeof(IReadOnlyList<PriceResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<PriceResult>>> Compare(
        [FromQuery(Name = "q")] string? query,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Problem("Search query is required.", statusCode: 400);

        var results = await _priceComparison.CompareAsync(query.Trim(), ct);
        return Ok(results);
    }
}

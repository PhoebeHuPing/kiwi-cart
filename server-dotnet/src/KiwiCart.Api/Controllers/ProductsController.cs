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
    private readonly IBucketService _bucketService;

    public ProductsController(IPriceComparisonService priceComparison, IBucketService bucketService)
    {
        _priceComparison = priceComparison;
        _bucketService = bucketService;
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

    [HttpPost("compare-bucket")]
    [ProducesResponseType(typeof(IReadOnlyList<BucketCompareResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<BucketCompareResult>>> CompareBucket(
        [FromBody] BucketCompareRequest request,
        CancellationToken ct)
    {
        if (request.Items is null || request.Items.Count == 0)
            return Problem("At least one item is required.", statusCode: 400);

        if (request.Items.Any(i => string.IsNullOrWhiteSpace(i.Name)))
            return Problem("All items must have a name.", statusCode: 400);

        var results = await _bucketService.CompareAsync(request.Items, ct);
        return Ok(results);
    }
}

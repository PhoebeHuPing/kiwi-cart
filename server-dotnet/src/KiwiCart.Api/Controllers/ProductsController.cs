using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using System.Security.Claims;

namespace KiwiCart.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IPriceComparisonService _priceComparison;
    private readonly IBucketService _bucketService;
    private readonly IFavoritesService _favoritesService;
    private readonly AppDbContext _db;

    public ProductsController(
        IPriceComparisonService priceComparison,
        IBucketService bucketService,
        IFavoritesService favoritesService,
        AppDbContext db)
    {
        _priceComparison = priceComparison;
        _bucketService = bucketService;
        _favoritesService = favoritesService;
        _db = db;
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

    [Authorize]
    [HttpGet("favorites")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetFavorites(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        return Ok(await _favoritesService.GetFavoritesAsync(userId, ct));
    }

    [Authorize]
    [HttpPost("favorites")]
    public async Task<ActionResult<object>> ToggleFavorite(
        [FromBody] FavoriteRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Problem("Product name is required.", statusCode: 400);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var (action, name) = await _favoritesService.ToggleAsync(userId, request.Name, ct);
        return Ok(new { action, name });
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var product = await _db.Products.FindAsync(new object[] { id }, ct);
        if (product is null) return NotFound();
        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public class FavoriteRequest
{
    public string Name { get; set; } = string.Empty;
}

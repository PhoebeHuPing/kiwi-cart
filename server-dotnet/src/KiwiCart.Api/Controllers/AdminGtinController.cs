using KiwiCart.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiwiCart.Api.Controllers;

/// <summary>
/// Admin-only endpoints to backfill the product_gtins table on demand. These
/// deep-fetch from a store and are intentionally kept off the user search path.
/// Authorized via Auth0 with the "admin" role.
/// </summary>
[ApiController]
[Route("api/v1/admin/gtins")]
[Produces("application/json")]
[Authorize(Roles = "admin")]
public class AdminGtinController : ControllerBase
{
    private readonly IGtinBackfillService _backfill;

    public AdminGtinController(IGtinBackfillService backfill)
    {
        _backfill = backfill;
    }

    /// <summary>
    /// Backfill Woolworths GTINs for all products matching a search term.
    /// Example: POST /api/v1/admin/gtins/woolworths/by-name { "q": "milk" }
    /// </summary>
    [HttpPost("woolworths/by-name")]
    [ProducesResponseType(typeof(GtinBackfillResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GtinBackfillResult>> BackfillByName(
        [FromBody] BackfillByNameRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Q))
            return BadRequest(new { error = "q is required" });

        var result = await _backfill.BackfillWoolworthsByNameAsync(request.Q.Trim(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Backfill a single Woolworths product's GTIN by its sku.
    /// Example: POST /api/v1/admin/gtins/woolworths/by-sku { "sku": "282768" }
    /// </summary>
    [HttpPost("woolworths/by-sku")]
    [ProducesResponseType(typeof(GtinBackfillResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GtinBackfillResult>> BackfillBySku(
        [FromBody] BackfillBySkuRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
            return BadRequest(new { error = "sku is required" });

        var result = await _backfill.BackfillWoolworthsBySkuAsync(request.Sku.Trim(), ct);
        return Ok(result);
    }

    /// <summary>
    /// API 3: resolve missing GTINs for Foodstuffs rows (product id recorded but
    /// gtin still null). Fetches each product's GTIN from the store detail
    /// endpoint, rate-limited by delayMs between calls.
    /// Example: POST /api/v1/admin/gtins/foodstuffs/backfill-missing { "count": 20, "delayMs": 500 }
    /// </summary>
    [HttpPost("foodstuffs/backfill-missing")]
    [ProducesResponseType(typeof(GtinBackfillResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GtinBackfillResult>> BackfillMissingFoodstuffs(
        [FromBody] BackfillMissingRequest request, CancellationToken ct)
    {
        if (request.Count <= 0)
            return BadRequest(new { error = "count must be greater than 0" });

        // Clamp to a sane range to avoid a runaway batch.
        var count = Math.Min(request.Count, 500);
        var delayMs = Math.Clamp(request.DelayMs, 0, 5000);

        var result = await _backfill.BackfillMissingFoodstuffsGtinsAsync(count, delayMs, ct);
        return Ok(result);
    }
}

public class BackfillByNameRequest
{
    public string? Q { get; set; }
}

public class BackfillBySkuRequest
{
    public string? Sku { get; set; }
}

public class BackfillMissingRequest
{
    public int Count { get; set; } = 20;
    public int DelayMs { get; set; } = 500;
}

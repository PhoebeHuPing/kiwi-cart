using KiwiCart.Core.Entities;
using KiwiCart.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KiwiCart.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class StoresController : ControllerBase
{
    private readonly IStoreService _storeService;

    public StoresController(IStoreService storeService) => _storeService = storeService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Store>>> GetAll(CancellationToken ct)
        => Ok(await _storeService.GetAllAsync(ct));

    [HttpGet("nearby")]
    [ProducesResponseType(typeof(IReadOnlyList<StoreWithDistance>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<StoreWithDistance>>> GetNearby(
        [FromQuery] double? lat, [FromQuery] double? lng,
        [FromQuery] double radius = 5,
        CancellationToken ct = default)
    {
        if (lat is null || lng is null)
            return Problem("lat and lng query parameters are required.", statusCode: 400);

        var results = await _storeService.GetNearbyAsync(lat.Value, lng.Value, radius, ct);
        return Ok(results);
    }
}

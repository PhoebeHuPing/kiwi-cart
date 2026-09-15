using KiwiCart.Core.Interfaces;
using KiwiCart.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiwiCart.Api.Controllers;

/// <summary>
/// Admin-only endpoints to refresh retailer store lists into the stores table.
/// Authorized via Auth0 with the "admin" role.
/// </summary>
[ApiController]
[Route("api/v1/admin/stores")]
[Produces("application/json")]
[Authorize(Policy = AdminAuthorization.PolicyName)]
public class AdminStoresController : ControllerBase
{
    private readonly IStoreSyncService _sync;

    public AdminStoresController(IStoreSyncService sync)
    {
        _sync = sync;
    }

    /// <summary>
    /// Fetch all Pak'nSave stores and upsert them into the stores table.
    /// Example: POST /api/v1/admin/stores/paknsave/sync
    /// </summary>
    [HttpPost("paknsave/sync")]
    [ProducesResponseType(typeof(StoreUpsertResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StoreUpsertResult>> SyncPakNSave(CancellationToken ct)
    {
        var result = await _sync.SyncPakNSaveStoresAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Fetch all New World stores and upsert them into the stores table.
    /// Example: POST /api/v1/admin/stores/newworld/sync
    /// </summary>
    [HttpPost("newworld/sync")]
    [ProducesResponseType(typeof(StoreUpsertResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StoreUpsertResult>> SyncNewWorld(CancellationToken ct)
    {
        var result = await _sync.SyncNewWorldStoresAsync(ct);
        return Ok(result);
    }
}

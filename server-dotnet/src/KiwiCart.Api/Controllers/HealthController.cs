using Microsoft.AspNetCore.Mvc;

namespace KiwiCart.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "ok",
            backend = "dotnet",
            framework = "ASP.NET Core 8"
        });
    }
}

using Microsoft.AspNetCore.Mvc;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/diagnostics")]
public class DiagnosticsController : ControllerBase
{
    /// <summary>Health check — retourne l'état du service Identity.</summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "DotnetNiger.Identity",
            timestamp = DateTime.UtcNow
        });
    }
}

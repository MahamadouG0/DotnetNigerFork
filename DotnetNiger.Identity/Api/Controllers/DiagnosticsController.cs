// Controleur API Identity: DiagnosticsController
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/diagnostics")]
// Endpoints publics pour tester le service.
public class DiagnosticsController : ControllerBase
{
	[HttpGet("ping")]
	[AllowAnonymous]
	public IActionResult Ping()
	{
		return Ok(new { status = "ok" });
	}

	[HttpGet("health")]
	[AllowAnonymous]
	public IActionResult Health()
	{
		return Ok(new { status = "healthy" });
	}
}

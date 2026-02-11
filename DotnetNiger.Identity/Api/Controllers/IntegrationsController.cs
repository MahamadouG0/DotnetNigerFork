// Controleur API Identity: IntegrationsController
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/integrations")]
// Endpoints reserves aux integrateurs (API key).
public class IntegrationsController : ControllerBase
{
	[HttpGet("ping")]
	[Authorize(Policy = "ApiKeyOnly")]
	public IActionResult Ping()
	{
		return Ok(new { status = "ok", auth = "api_key" });
	}
}

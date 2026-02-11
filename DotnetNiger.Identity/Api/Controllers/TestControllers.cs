// Controleur API Identity: TestControllers
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/")]
public class TestController : ControllerBase
{
    [HttpGet("test")]
    [AllowAnonymous]
    public IActionResult GetTest()
    {
        return Ok(new[] { new { Id = 1, Title = "Test Ok" } });
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult GetHealth()
    {
        return Ok(new[] { new { Id = 2, Title = "Health Ok" } });
    }
}

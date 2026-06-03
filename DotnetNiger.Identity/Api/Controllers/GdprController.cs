using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static OpenIddict.Abstractions.OpenIddictConstants;
using DotnetNiger.Identity.Application.DTOs;
using DotnetNiger.Identity.Application.Services;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/account")]
[Authorize]
public class GdprController : ControllerBase
{
    private readonly GdprService _gdprService;

    public GdprController(GdprService gdprService) => _gdprService = gdprService;

    [HttpPost("consent")]
    public async Task<IActionResult> RecordConsent([FromBody] ConsentRequest request)
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        await _gdprService.RecordConsentAsync(Guid.Parse(userId), request.ConsentType, request.ConsentVersion, request.Granted, ip, userAgent);
        return Ok(new { message = "Consentement enregistré." });
    }

    [HttpGet("consent")]
    public async Task<ActionResult<List<ConsentResponse>>> GetConsentHistory()
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();

        var consents = await _gdprService.GetLatestConsentsAsync(Guid.Parse(userId));
        return Ok(consents);
    }

    [HttpGet("data")]
    public async Task<IActionResult> ExportData()
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();

        var zipData = await _gdprService.ExportUserDataAsync(Guid.Parse(userId));
        return File(zipData, "application/zip", $"dotnetniger-export-{DateTime.UtcNow:yyyy-MM-dd}.zip");
    }

    [HttpPost("forget-me")]
    public async Task<ActionResult<ForgetMeResponse>> ForgetMe()
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (userId == null) return Unauthorized();

        await _gdprService.ForgetMeAsync(Guid.Parse(userId));
        return Ok(new ForgetMeResponse("Vos données ont été anonymisées conformément au RGPD.", DateTime.UtcNow));
    }
}

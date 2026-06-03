using Asp.Versioning;
using DotnetNiger.Community.Application;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetNiger.Community.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class NewsletterController(INewsletterService newsletterService) : ControllerBase
{
    [HttpPost("subscribe")]
    [AllowAnonymous]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        try
        {
            var result = await newsletterService.SubscribeAsync(request);
            return Ok(new { Success = true, Data = result });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Success = false, Message = ex.Message });
        }
    }

    [HttpPost("unsubscribe")]
    [AllowAnonymous]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
    {
        var result = await newsletterService.UnsubscribeAsync(request);
        if (!result)
            return NotFound(new { Success = false, Message = "Abonnement non trouvé ou déjà désabonné" });
        return Ok(new { Success = true, Message = "Désabonnement réussi" });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, ValidationConstants.MaxPageSize);
        var result = await newsletterService.GetAllAsync(page, pageSize);
        return Ok(new { Success = true, Data = result });
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetActiveCount()
    {
        var count = await newsletterService.GetActiveCountAsync();
        return Ok(new { Success = true, Data = new { ActiveCount = count } });
    }
}

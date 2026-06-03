using Asp.Versioning;
using System.Security.Claims;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetNiger.Community.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ProfileController(IProfileService profileService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Get()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await profileService.GetAsync(userId);
        if (profile is null) return NotFound(new { Success = false, Message = "Profile not found" });
        return Ok(new { Success = true, Data = profile });
    }

    [HttpPut("me")]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await profileService.UpdateAsync(userId, request);
        return Ok(new { Success = true, Data = profile });
    }

    [HttpGet("social-links")]
    public async Task<IActionResult> GetSocialLinks()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await profileService.GetAsync(userId);
        return Ok(new { Success = true, Data = profile?.SocialLinks ?? [] });
    }

    [HttpPost("social-links")]
    public async Task<IActionResult> AddSocialLink([FromBody] AddSocialLinkRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var link = await profileService.AddSocialLinkAsync(userId, request);
        return Ok(new { Success = true, Data = link });
    }

    [HttpDelete("social-links/{id:guid}")]
    public async Task<IActionResult> DeleteSocialLink(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var deleted = await profileService.DeleteSocialLinkAsync(userId, id);
        if (!deleted) return NotFound(new { Success = false, Message = "Social link not found" });
        return Ok(new { Success = true, Message = "Social link deleted" });
    }
}



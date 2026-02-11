// Controleur API Identity: SocialLinksController
using System.Security.Claims;
using Asp.Versioning;
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/social-links")]
[Authorize]
// Endpoints pour les liens sociaux de l'utilisateur connecte.
public class SocialLinksController : ControllerBase
{
	// Endpoints proteges pour les liens sociaux.
	private readonly ISocialLinkService _socialLinkService;

	public SocialLinksController(ISocialLinkService socialLinkService)
	{
		_socialLinkService = socialLinkService;
	}

	[HttpGet]
	public async Task<ActionResult<IReadOnlyList<SocialLinkDto>>> GetMyLinks()
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		var links = await _socialLinkService.GetForUserAsync(userId.Value);
		return Ok(links);
	}

	[HttpPost]
	public async Task<ActionResult<SocialLinkDto>> AddLink([FromBody] AddSocialLinkRequest request)
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		var link = await _socialLinkService.AddAsync(userId.Value, request);
		return Ok(link);
	}

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> DeleteLink(Guid id)
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		await _socialLinkService.DeleteAsync(userId.Value, id);
		return NoContent();
	}

	private Guid? GetUserId()
	{
		var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrWhiteSpace(userIdValue))
		{
			return null;
		}

		return Guid.TryParse(userIdValue, out var userId) ? userId : null;
	}
}

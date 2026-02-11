// Controleur API Identity: TokensController
using Asp.Versioning;
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tokens")]
[Authorize]
public class TokensController : ControllerBase
{
	// Endpoints proteges pour la gestion des tokens.
	private readonly ITokenService _tokenService;

	public TokensController(ITokenService tokenService)
	{
		_tokenService = tokenService;
	}

	[HttpPost("refresh")]
	[AllowAnonymous]
	public async Task<ActionResult<AuthDto>> Refresh([FromBody] RefreshTokenRequest request)
	{
		var result = await _tokenService.RefreshAsync(request);
		return Ok(result);
	}

	[HttpPost("logout")]
	public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		await _tokenService.LogoutAsync(userId.Value, request);
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

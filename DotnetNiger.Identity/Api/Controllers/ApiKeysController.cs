// Controleur API Identity: ApiKeysController
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
[Route("api/v{version:apiVersion}/api-keys")]
[Authorize]
// Endpoints pour la gestion des cles API.
public class ApiKeysController : ControllerBase
{
	private readonly IApiKeyService _apiKeyService;

	public ApiKeysController(IApiKeyService apiKeyService)
	{
		_apiKeyService = apiKeyService;
	}

	[HttpGet]
	public async Task<ActionResult<IReadOnlyList<ApiKeyDto>>> List()
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		var result = await _apiKeyService.ListAsync(userId.Value);
		return Ok(result);
	}

	[HttpPost]
	public async Task<ActionResult<ApiKeySecretDto>> Create([FromBody] CreateApiKeyRequest request)
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		var result = await _apiKeyService.CreateAsync(userId.Value, request);
		return Ok(result);
	}

	[HttpPost("{apiKeyId:guid}/rotate")]
	public async Task<ActionResult<ApiKeySecretDto>> Rotate(Guid apiKeyId)
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		var result = await _apiKeyService.RotateAsync(userId.Value, apiKeyId);
		return Ok(result);
	}

	[HttpDelete("{apiKeyId:guid}")]
	public async Task<IActionResult> Revoke(Guid apiKeyId)
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		await _apiKeyService.RevokeAsync(userId.Value, apiKeyId);
		return NoContent();
	}

	[HttpPost("revoke-all")]
	public async Task<IActionResult> RevokeAll()
	{
		var userId = GetUserId();
		if (userId == null)
		{
			return Unauthorized();
		}

		await _apiKeyService.RevokeAllAsync(userId.Value);
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

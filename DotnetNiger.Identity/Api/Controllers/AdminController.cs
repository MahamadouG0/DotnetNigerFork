// Controleur API Identity: AdminController
using Asp.Versioning;
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize(Roles = "Admin")]
// Endpoints d'administration et supervision des utilisateurs.
public class AdminController : ControllerBase
{
	private readonly IAdminService _adminService;
	private readonly IUserService _userService;
	private readonly ILoginHistoryService _loginHistoryService;

	public AdminController(
		IAdminService adminService,
		IUserService userService,
		ILoginHistoryService loginHistoryService)
	{
		_adminService = adminService;
		_userService = userService;
		_loginHistoryService = loginHistoryService;
	}

	[HttpGet("users")]
	public async Task<ActionResult<PaginatedDto<UserSummaryDto>>> GetUsers(
		[FromQuery] string? search,
		[FromQuery] bool? isActive,
		[FromQuery] bool? emailConfirmed,
		[FromQuery] string? role,
		[FromQuery] DateTime? createdFrom,
		[FromQuery] DateTime? createdTo,
		[FromQuery] string? sortBy,
		[FromQuery] string? sortDirection,
		[FromQuery] int skip = 0,
		[FromQuery] int take = 20)
	{
		if (skip < 0)
		{
			skip = 0;
		}

		if (take <= 0)
		{
			take = 20;
		}

		if (take > 100)
		{
			take = 100;
		}

		var result = await _adminService.GetUsersAsync(
			search,
			isActive,
			emailConfirmed,
			role,
			createdFrom,
			createdTo,
			sortBy,
			sortDirection,
			skip,
			take);
		return Ok(result);
	}

	[HttpGet("users/{userId:guid}")]
	public async Task<ActionResult<UserDto>> GetUser(Guid userId)
	{
		var profile = await _userService.GetProfileAsync(userId);
		return Ok(profile);
	}

	[HttpPut("users/{userId:guid}/status")]
	public async Task<IActionResult> UpdateUserStatus(Guid userId, [FromBody] UpdateUserStatusRequest request)
	{
		await _adminService.SetUserActiveAsync(userId, request.IsActive);
		return NoContent();
	}

	[HttpGet("users/{userId:guid}/login-history")]
	public async Task<ActionResult<PaginatedDto<LoginHistoryDto>>> GetUserLoginHistory(
		Guid userId,
		[FromQuery] int skip = 0,
		[FromQuery] int take = 20)
	{
		if (skip < 0)
		{
			skip = 0;
		}

		if (take <= 0)
		{
			take = 20;
		}

		if (take > 100)
		{
			take = 100;
		}

		var result = await _loginHistoryService.GetUserHistoryAsync(userId, skip, take);
		return Ok(result);
	}

	[HttpGet("api-keys")]
	public async Task<ActionResult<PaginatedDto<ApiKeyAuditDto>>> GetApiKeys(
		[FromQuery] string? search,
		[FromQuery] Guid? userId,
		[FromQuery] bool? isActive,
		[FromQuery] bool? expired,
		[FromQuery] DateTime? createdFrom,
		[FromQuery] DateTime? createdTo,
		[FromQuery] DateTime? lastUsedFrom,
		[FromQuery] DateTime? lastUsedTo,
		[FromQuery] string? sortBy,
		[FromQuery] string? sortDirection,
		[FromQuery] int skip = 0,
		[FromQuery] int take = 20)
	{
		if (skip < 0)
		{
			skip = 0;
		}

		if (take <= 0)
		{
			take = 20;
		}

		if (take > 100)
		{
			take = 100;
		}

		var result = await _adminService.GetApiKeysAsync(
			search,
			userId,
			isActive,
			expired,
			createdFrom,
			createdTo,
			lastUsedFrom,
			lastUsedTo,
			sortBy,
			sortDirection,
			skip,
			take);
		return Ok(result);
	}

	[HttpPost("api-keys/{apiKeyId:guid}/rotate")]
	public async Task<ActionResult<ApiKeySecretDto>> RotateApiKey(Guid apiKeyId)
	{
		var result = await _adminService.RotateApiKeyAsync(apiKeyId);
		return Ok(result);
	}

	[HttpDelete("api-keys/{apiKeyId:guid}")]
	public async Task<IActionResult> RevokeApiKey(Guid apiKeyId)
	{
		await _adminService.RevokeApiKeyAsync(apiKeyId);
		return NoContent();
	}

	[HttpPost("users/{userId:guid}/api-keys/revoke-all")]
	public async Task<IActionResult> RevokeUserApiKeys(Guid userId)
	{
		await _adminService.RevokeUserApiKeysAsync(userId);
		return NoContent();
	}

	[HttpGet("audit-logs")]
	public async Task<ActionResult<PaginatedDto<AdminAuditLogDto>>> GetAuditLogs(
		[FromQuery] Guid? adminUserId,
		[FromQuery] string? action,
		[FromQuery] string? targetType,
		[FromQuery] DateTime? createdFrom,
		[FromQuery] DateTime? createdTo,
		[FromQuery] int skip = 0,
		[FromQuery] int take = 20)
	{
		// Filtrage simple pour retrouver les actions sensibles.
		if (skip < 0)
		{
			skip = 0;
		}

		if (take <= 0)
		{
			take = 20;
		}

		if (take > 100)
		{
			take = 100;
		}

		var result = await _adminService.GetAdminAuditLogsAsync(
			adminUserId,
			action,
			targetType,
			createdFrom,
			createdTo,
			skip,
			take);
		return Ok(result);
	}
}

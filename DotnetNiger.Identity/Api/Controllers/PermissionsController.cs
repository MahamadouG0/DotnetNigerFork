// Controleur API Identity: PermissionsController
using Asp.Versioning;
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/permissions")]
[Authorize(Roles = "Admin")]
// Endpoints pour la gestion des permissions.
public class PermissionsController : ControllerBase
{
	private readonly IPermissionService _permissionService;

	public PermissionsController(IPermissionService permissionService)
	{
		_permissionService = permissionService;
	}

	[HttpGet]
	public async Task<ActionResult<IReadOnlyList<PermissionDto>>> GetPermissions()
	{
		var permissions = await _permissionService.GetAllAsync();
		return Ok(permissions);
	}

	[HttpPost]
	public async Task<ActionResult<PermissionDto>> CreatePermission([FromBody] AddPermissionRequest request)
	{
		var permission = await _permissionService.CreateAsync(request);
		return Ok(permission);
	}

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> DeletePermission(Guid id)
	{
		await _permissionService.DeleteAsync(id);
		return NoContent();
	}

	[HttpPost("assign")]
	public async Task<IActionResult> AssignPermission([FromBody] AssignPermissionRequest request)
	{
		await _permissionService.AssignToRoleAsync(request);
		return NoContent();
	}

	[HttpPost("remove")]
	public async Task<IActionResult> RemovePermission([FromBody] AssignPermissionRequest request)
	{
		await _permissionService.RemoveFromRoleAsync(request);
		return NoContent();
	}

	[HttpGet("role/{roleId:guid}")]
	public async Task<ActionResult<IReadOnlyList<PermissionDto>>> GetRolePermissions(Guid roleId)
	{
		var permissions = await _permissionService.GetRolePermissionsAsync(roleId);
		return Ok(permissions);
	}
}

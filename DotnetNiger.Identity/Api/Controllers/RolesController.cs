// Controleur API Identity: RolesController
using Asp.Versioning;
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
[Authorize(Roles = "Admin")]
// Endpoints pour la gestion des roles.
public class RolesController : ControllerBase
{
	// Endpoints proteges pour les roles.
	private readonly IRoleService _roleService;

	public RolesController(IRoleService roleService)
	{
		_roleService = roleService;
	}

	[HttpGet]
	public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles()
	{
		var roles = await _roleService.GetAllAsync();
		return Ok(roles);
	}

	[HttpPost]
	public async Task<ActionResult<RoleDto>> CreateRole([FromBody] AddRoleRequest request)
	{
		var role = await _roleService.CreateAsync(request);
		return Ok(role);
	}

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> DeleteRole(Guid id)
	{
		await _roleService.DeleteAsync(id);
		return NoContent();
	}

	[HttpPost("assign")]
	public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
	{
		await _roleService.AssignToUserAsync(request);
		return NoContent();
	}

	[HttpPost("remove")]
	public async Task<IActionResult> RemoveRole([FromBody] AssignRoleRequest request)
	{
		await _roleService.RemoveFromUserAsync(request);
		return NoContent();
	}

	[HttpGet("user/{userId:guid}")]
	public async Task<ActionResult<IReadOnlyList<string>>> GetUserRoles(Guid userId)
	{
		var roles = await _roleService.GetUserRolesAsync(userId);
		return Ok(roles);
	}
}

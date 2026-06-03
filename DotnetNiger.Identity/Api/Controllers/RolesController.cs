using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DotnetNiger.Identity.Application.DTOs;
using DotnetNiger.Identity.Application.Services;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]

[Route("api/v{version:apiVersion}/{tenantId:guid}/roles")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly RoleService _roleService;

    public RolesController(RoleService roleService) => _roleService = roleService;

    /// <summary>Crée un nouveau rôle dans le tenant spécifié.</summary>
    [HttpPost]
    public async Task<ActionResult<RoleResponse>> Create(Guid tenantId, [FromBody] CreateRoleRequest request)
    {
        if (request.TenantId != tenantId)
            return BadRequest(new ErrorResponse("Tenant mismatch"));

        var role = await _roleService.CreateAsync(request);
        return CreatedAtAction(nameof(GetAll), new { tenantId }, role);
    }

    /// <summary>Liste tous les rôles du tenant.</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<RoleResponse>>> GetAll(Guid tenantId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var roles = await _roleService.GetByTenantAsync(tenantId, new PaginationQuery(page, pageSize));
        return Ok(roles);
    }

    /// <summary>Met à jour un rôle (description uniquement).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RoleResponse>> Update(Guid tenantId, Guid id,
        [FromBody] UpdateRoleRequest request)
    {
        var role = await _roleService.UpdateAsync(id, request);
        return Ok(role);
    }

    /// <summary>Supprime un rôle.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid tenantId, Guid id)
    {
        await _roleService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Assigne un rôle à un utilisateur.</summary>
    [HttpPost("{roleId:guid}/users/{userId:guid}")]
    public async Task<IActionResult> AssignUser(Guid tenantId, Guid roleId, Guid userId)
    {
        await _roleService.AssignToUserAsync(userId, roleId);
        return NoContent();
    }

    /// <summary>Retire un rôle à un utilisateur.</summary>
    [HttpDelete("{roleId:guid}/users/{userId:guid}")]
    public async Task<IActionResult> RemoveUser(Guid tenantId, Guid roleId, Guid userId)
    {
        await _roleService.RemoveFromUserAsync(userId, roleId);
        return NoContent();
    }

    /// <summary>Retourne les rôles d'un utilisateur.</summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<List<RoleResponse>>> GetUserRoles(Guid tenantId, Guid userId)
    {
        var roles = await _roleService.GetUserRolesAsync(userId);
        return Ok(roles);
    }
}

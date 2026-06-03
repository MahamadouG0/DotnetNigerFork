using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DotnetNiger.Identity.Application.DTOs;
using DotnetNiger.Identity.Application.Services;

namespace DotnetNiger.Identity.Api.Controllers;

/// <summary>Gestion des permissions : CRUD, groupage, assignation aux rôles (Admin).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{tenantId:guid}/permissions")]
[Authorize(Roles = "Admin")]
public class PermissionsController : ControllerBase
{
    private readonly PermissionService _permissionService;

    public PermissionsController(PermissionService permissionService) => _permissionService = permissionService;

    /// <summary>Crée une nouvelle permission dans le tenant.</summary>
    [HttpPost]
    public async Task<ActionResult<PermissionResponse>> Create(Guid tenantId,
        [FromBody] CreatePermissionRequest request)
    {
        if (request.TenantId != tenantId)
            return BadRequest(new ErrorResponse("Tenant mismatch"));

        var permission = await _permissionService.CreateAsync(request);
        return CreatedAtAction(nameof(GetAll), new { tenantId }, permission);
    }

    /// <summary>Liste toutes les permissions du tenant.</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<PermissionResponse>>> GetAll(Guid tenantId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var permissions = await _permissionService.GetByTenantAsync(tenantId, new PaginationQuery(page, pageSize));
        return Ok(permissions);
    }

    /// <summary>Liste les permissions groupées par catégorie.</summary>
    [HttpGet("grouped")]
    public async Task<ActionResult<List<PermissionGroupResponse>>> GetGrouped(Guid tenantId)
    {
        var grouped = await _permissionService.GetGroupedByTenantAsync(tenantId);
        return Ok(grouped);
    }

    /// <summary>Supprime une permission.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid tenantId, Guid id)
    {
        await _permissionService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Assigne des permissions à un rôle.</summary>
    [HttpPost("assign")]
    public async Task<IActionResult> AssignToRole(Guid tenantId,
        [FromBody] AssignPermissionsRequest request)
    {
        await _permissionService.AssignToRoleAsync(request.RoleId, request.PermissionIds.ToList());
        return NoContent();
    }
}

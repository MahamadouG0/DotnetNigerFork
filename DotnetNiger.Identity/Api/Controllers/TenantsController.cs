using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DotnetNiger.Identity.Application.DTOs;
using DotnetNiger.Identity.Application.Services;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]

[Route("api/v{version:apiVersion}/admin/tenants")]
[Authorize(Roles = "Admin")]
public class TenantsController : ControllerBase
{
    private readonly TenantService _tenantService;

    public TenantsController(TenantService tenantService) => _tenantService = tenantService;

    /// <summary>Crée un nouveau tenant avec un compte admin par défaut.</summary>
    [HttpPost]
    public async Task<ActionResult<TenantResponse>> Create([FromBody] CreateTenantRequest request)
    {
        var tenant = await _tenantService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, tenant);
    }

    /// <summary>Liste tous les tenants de la plateforme (paginated).</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<TenantResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _tenantService.GetAllAsync(new PaginationQuery(page, pageSize));
        return Ok(result);
    }

    /// <summary>Retourne un tenant par son ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantResponse>> GetById(Guid id)
    {
        var tenant = await _tenantService.GetByIdAsync(id);
        if (tenant == null) return NotFound(new ErrorResponse("Tenant non trouvé"));
        return Ok(tenant);
    }

    /// <summary>Retourne un tenant par son slug.</summary>
    [HttpGet("by-slug/{slug}")]
    public async Task<ActionResult<TenantResponse>> GetBySlug(string slug)
    {
        var tenant = await _tenantService.GetBySlugAsync(slug);
        if (tenant == null) return NotFound(new ErrorResponse("Tenant non trouvé"));
        return Ok(tenant);
    }

    /// <summary>Met à jour les informations d'un tenant.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TenantResponse>> Update(Guid id, [FromBody] UpdateTenantRequest request)
    {
        var tenant = await _tenantService.UpdateAsync(id, request);
        return Ok(tenant);
    }

    /// <summary>Supprime un tenant et toutes ses données associées.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _tenantService.DeleteAsync(id);
        return NoContent();
    }
}

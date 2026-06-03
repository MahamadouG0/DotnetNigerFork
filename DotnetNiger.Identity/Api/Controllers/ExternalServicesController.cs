using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotnetNiger.Identity.Application.Services;
using DotnetNiger.Identity.Application.DTOs;
using DotnetNiger.Identity.Api.Authentication;
using DotnetNiger.Identity.Infrastructure;
using OpenIddict.Validation.AspNetCore;

namespace DotnetNiger.Identity.Api.Controllers;

/// <summary>Enregistrement et gestion des services externes, résolution de slug pour le Gateway.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/external-services")]
public class ExternalServicesController : ControllerBase
{
    private readonly ExternalServiceService _service;
    private readonly IdentityDbContext _db;

    public ExternalServicesController(ExternalServiceService service, IdentityDbContext db)
    {
        _service = service;
        _db = db;
    }

    private async Task<(Guid tenantId, Guid? apiKeyId)> GetAuthInfoAsync()
    {
        var tenantId = Guid.Parse(User.FindFirstValue("tenant_id")!);
        Guid? apiKeyId = null;
        var keyClaim = User.FindFirstValue("api_key_id");
        if (!string.IsNullOrEmpty(keyClaim))
            apiKeyId = Guid.Parse(keyClaim);
        else
        {
            var key = await _db.TenantApiKeys
                .Where(k => k.TenantId == tenantId && k.IsActive)
                .OrderBy(k => k.CreatedAt)
                .FirstOrDefaultAsync();
            apiKeyId = key?.Id;
        }
        return (tenantId, apiKeyId);
    }

    private const string BothSchemes = "ApiKey," + OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;

    /// <summary>Enregistre une API externe sur la plateforme.</summary>
    [HttpPost("register")]
    [Authorize(AuthenticationSchemes = BothSchemes)]
    public async Task<ActionResult<ExternalServiceResponse>> Register(
        [FromBody] RegisterExternalServiceRequest request)
    {
        var (tenantId, apiKeyId) = await GetAuthInfoAsync();
        if (apiKeyId == null)
            return BadRequest(new { error = "Aucune clé API active trouvée pour ce tenant" });

        try
        {
            var result = await _service.RegisterAsync(tenantId, apiKeyId.Value, request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>Liste tous les services externes du développeur connecté.</summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = BothSchemes)]
    public async Task<ActionResult<PaginatedResponse<ExternalServiceResponse>>> GetMyServices(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (tenantId, _) = await GetAuthInfoAsync();
        var services = await _service.GetByTenantAsync(tenantId, new PaginationQuery(page, pageSize));
        return Ok(services);
    }

    /// <summary>Retourne un service externe par son ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(AuthenticationSchemes = BothSchemes)]
    public async Task<ActionResult<ExternalServiceResponse>> GetById(Guid id)
    {
        var (tenantId, _) = await GetAuthInfoAsync();
        try
        {
            var result = await _service.GetByIdAsync(tenantId, id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Met à jour l'URL, la description ou le health endpoint d'un service.</summary>
    [HttpPatch("{id:guid}")]
    [Authorize(AuthenticationSchemes = BothSchemes)]
    public async Task<ActionResult<ExternalServiceResponse>> Update(
        Guid id, [FromBody] UpdateExternalServiceRequest request)
    {
        var (tenantId, _) = await GetAuthInfoAsync();
        try
        {
            var result = await _service.UpdateAsync(tenantId, id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Supprime un service externe.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = BothSchemes)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (tenantId, _) = await GetAuthInfoAsync();
        try
        {
            await _service.DeleteAsync(tenantId, id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Résout un slug en URL de base. Utilisé par le Gateway pour le proxy /ext/{slug}/**.</summary>
    [AllowAnonymous]
    [HttpGet("by-slug/{slug}")]
    public async Task<ActionResult<ServiceLookupResult>> ResolveSlug(string slug)
    {
        var result = await _service.ResolveSlugAsync(slug);
        if (result == null)
            return NotFound(new { error = "Service not found or not active" });
        return Ok(result);
    }

    /// <summary>Liste tous les services actifs. Utilisé en interne par le health check du Gateway.</summary>
    [HttpGet("_internal/active")]
    [InternalApiKeyAuth]
    public async Task<ActionResult<List<ExternalServiceResponse>>> GetAllActive()
    {
        var services = await _service.GetAllActiveAsync();
        return Ok(services);
    }

    /// <summary>Reçoit le résultat d'un health check du Gateway et met à jour le statut.</summary>
    [HttpPost("_internal/{id:guid}/health-result")]
    [InternalApiKeyAuth]
    public async Task<IActionResult> ReportHealthResult(Guid id, [FromBody] HealthCheckResultDto result)
    {
        await _service.UpdateHealthStatusAsync(id, result.IsHealthy);
        return Ok();
    }
}

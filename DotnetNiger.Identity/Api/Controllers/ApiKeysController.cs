using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DotnetNiger.Identity.Application.DTOs;
using DotnetNiger.Identity.Application.Services;

namespace DotnetNiger.Identity.Api.Controllers;

/// <summary>Gestion des clés API : CRUD, rotation par tenant (Admin).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/tenants/{tenantId:guid}/api-keys")]
[Authorize(Roles = "Admin")]
public class ApiKeysController : ControllerBase
{
    private readonly TenantApiKeyService _apiKeyService;

    public ApiKeysController(TenantApiKeyService apiKeyService) => _apiKeyService = apiKeyService;

    /// <summary>Liste toutes les clés API d'un tenant.</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<TenantApiKeyResponse>>> GetAll(Guid tenantId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var keys = await _apiKeyService.GetApiKeysAsync(tenantId, new PaginationQuery(page, pageSize));
        return Ok(keys);
    }

    /// <summary>Retourne une clé API par son ID.</summary>
    [HttpGet("{keyId:guid}")]
    public async Task<ActionResult<TenantApiKeyResponse>> GetById(Guid tenantId, Guid keyId)
    {
        var key = await _apiKeyService.GetApiKeyByIdAsync(tenantId, keyId);
        return Ok(key);
    }

    /// <summary>Crée une nouvelle clé API pour le tenant.</summary>
    [HttpPost]
    public async Task<ActionResult<TenantApiKeyCreatedResponse>> Create(Guid tenantId,
        [FromBody] CreateTenantApiKeyRequest request)
    {
        var result = await _apiKeyService.CreateApiKeyAsync(tenantId, request);
        return CreatedAtAction(nameof(GetById), new { tenantId, keyId = result.Key.Id }, result);
    }

    /// <summary>Rotation d'une clé API — génère un nouveau secret.</summary>
    [HttpPost("{keyId:guid}/rotate")]
    public async Task<ActionResult<TenantApiKeyCreatedResponse>> Rotate(Guid tenantId, Guid keyId)
    {
        var result = await _apiKeyService.RotateApiKeyAsync(tenantId, keyId);
        return Ok(result);
    }

    /// <summary>Supprime définitivement une clé API.</summary>
    [HttpDelete("{keyId:guid}")]
    public async Task<IActionResult> Delete(Guid tenantId, Guid keyId)
    {
        await _apiKeyService.DeleteApiKeyAsync(tenantId, keyId);
        return NoContent();
    }
}

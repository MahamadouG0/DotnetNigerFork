using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DotnetNiger.Identity.Application.DTOs;
using DotnetNiger.Identity.Application.Services;

namespace DotnetNiger.Identity.Api.Controllers;

/// <summary>Gestion des clients OAuth2 : CRUD par tenant (Admin).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/tenants/{tenantId:guid}/clients")]
[Authorize(Roles = "Admin")]
public class ClientsController : ControllerBase
{
    private readonly TenantClientService _clientService;

    public ClientsController(TenantClientService clientService) => _clientService = clientService;

    /// <summary>Liste tous les clients OAuth2 d'un tenant.</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<TenantClientResponse>>> GetAll(Guid tenantId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var clients = await _clientService.GetClientsAsync(tenantId, new PaginationQuery(page, pageSize));
        return Ok(clients);
    }

    /// <summary>Retourne un client OAuth2 par son ID.</summary>
    [HttpGet("{clientId:guid}")]
    public async Task<ActionResult<TenantClientResponse>> GetById(Guid tenantId, Guid clientId)
    {
        var client = await _clientService.GetClientByIdAsync(tenantId, clientId);
        return Ok(client);
    }

    /// <summary>Crée un nouveau client OAuth2 pour le tenant.</summary>
    [HttpPost]
    public async Task<ActionResult<TenantClientCreatedResponse>> Create(Guid tenantId,
        [FromBody] CreateTenantClientRequest request)
    {
        var result = await _clientService.CreateClientAsync(tenantId, request);
        return CreatedAtAction(nameof(GetById), new { tenantId, clientId = result.Client.Id }, result);
    }

    /// <summary>Met à jour un client OAuth2 (nom, URIs de redirection, type de flux).</summary>
    [HttpPut("{clientId:guid}")]
    public async Task<ActionResult<TenantClientResponse>> Update(Guid tenantId, Guid clientId,
        [FromBody] UpdateTenantClientRequest request)
    {
        var client = await _clientService.UpdateClientAsync(tenantId, clientId, request);
        return Ok(client);
    }

    /// <summary>Supprime un client OAuth2.</summary>
    [HttpDelete("{clientId:guid}")]
    public async Task<IActionResult> Delete(Guid tenantId, Guid clientId)
    {
        await _clientService.DeleteClientAsync(tenantId, clientId);
        return NoContent();
    }
}

using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs;

public record CreateRoleRequest(
    [Required] string Name,
    string? Description,
    [Required] Guid TenantId);

public record UpdateRoleRequest(
    string? Description);

using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs;

public record CreateTenantRequest(
    [Required] string Name,
    [Required] string Slug,
    string? Description);

public record UpdateTenantRequest(
    string? Name,
    string? Description,
    bool? IsActive);

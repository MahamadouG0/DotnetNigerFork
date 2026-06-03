using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs;

public record CreateTenantClientRequest(
    [Required][StringLength(100, MinimumLength = 1)] string ClientName,
    string? Description,
    string? RedirectUris,
    string? PostLogoutRedirectUris,
    string? AllowedGrantTypes);

public record UpdateTenantClientRequest(
    string? ClientName,
    string? Description,
    string? RedirectUris,
    string? PostLogoutRedirectUris,
    string? AllowedGrantTypes,
    bool? IsActive);

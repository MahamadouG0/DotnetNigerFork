using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs;

public record CreateTenantApiKeyRequest(
    [Required][StringLength(100, MinimumLength = 1)] string Name,
    string? Scopes,
    DateTime? ExpiresAt);

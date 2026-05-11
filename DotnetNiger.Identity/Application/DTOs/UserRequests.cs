using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs;

public record CreateUserRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    [Required] Guid TenantId,
    IList<string>? Roles = null);

public record UpdateUserRequest(
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    bool? IsActive);

public record ResendEmailConfirmationRequest(
    [Required][EmailAddress] string Email);

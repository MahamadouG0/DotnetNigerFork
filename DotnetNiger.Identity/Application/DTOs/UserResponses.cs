namespace DotnetNiger.Identity.Application.DTOs;

public record UserResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    Guid TenantId,
    bool IsActive,
    bool EmailConfirmed,
    DateTime CreatedAt,
    IList<string> Roles);

public record UserProfileResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    Guid? TenantId,
    IList<string> Roles);

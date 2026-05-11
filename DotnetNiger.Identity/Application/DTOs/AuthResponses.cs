namespace DotnetNiger.Identity.Application.DTOs;

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    Guid UserId,
    string Email,
    Guid? TenantId,
    IList<string> Roles);

public record UserInfoResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    Guid? TenantId,
    bool IsActive,
    IList<string> Roles,
    IList<string> Permissions,
    bool RememberMe = false);

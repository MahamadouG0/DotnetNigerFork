namespace DotnetNiger.Identity.Application.DTOs;

public record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid TenantId,
    int UserCount);

public record RoleWithPermissionsResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid TenantId,
    IList<string> Permissions);

namespace DotnetNiger.Identity.Application.DTOs;

public record PermissionResponse(
    Guid Id,
    string Name,
    string Category,
    Guid TenantId);

public record PermissionGroupResponse(
    string Category,
    IList<PermissionResponse> Permissions);

using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs;

public record CreatePermissionRequest(
    [Required] string Name,
    [Required] string Category,
    [Required] Guid TenantId);

public record AssignPermissionsRequest(
    [Required] Guid RoleId,
    [Required] IList<Guid> PermissionIds);

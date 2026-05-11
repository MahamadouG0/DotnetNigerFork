using DotnetNiger.Community.Application.DTOs;

namespace DotnetNiger.Community.Application.Services;

public interface IIdentityApiClient
{
    Task<List<UserDto>> GetUsersAsync();
    Task<UserDto?> GetUserAsync(Guid id);
    Task<bool> UpdateUserStatusAsync(Guid id, bool isActive);
    Task<List<RoleDto>> GetRolesAsync();
    Task<RoleDto?> CreateRoleAsync(string name);
    Task<List<PermissionDto>> GetPermissionsAsync();
    Task<PermissionDto?> CreatePermissionAsync(string name, string description);
    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> AssignRoleToUserAsync(Guid userId, string roleName);
}

using System.Net.Http.Json;
using DotnetNiger.Community.Application.DTOs;

namespace DotnetNiger.Community.Application.Services;

public class IdentityApiClient(HttpClient http) : IIdentityApiClient
{
    public async Task<List<UserDto>> GetUsersAsync()
    {
        var response = await http.GetAsync("api/v1/admin/users");
        if (!response.IsSuccessStatusCode) return [];
        var wrapped = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<List<UserDto>>>();
        return wrapped?.Data ?? [];
    }

    public async Task<UserDto?> GetUserAsync(Guid id)
    {
        var response = await http.GetAsync($"api/v1/admin/users/{id}");
        if (!response.IsSuccessStatusCode) return null;
        var wrapped = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<UserDto>>();
        return wrapped?.Data;
    }

    public async Task<bool> UpdateUserStatusAsync(Guid id, bool isActive)
    {
        var response = await http.PatchAsJsonAsync($"api/v1/admin/users/{id}/status", new { isActive });
        return response.IsSuccessStatusCode;
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var response = await http.GetAsync("api/v1/admin/roles");
        if (!response.IsSuccessStatusCode) return [];
        var wrapped = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<List<RoleDto>>>();
        return wrapped?.Data ?? [];
    }

    public async Task<RoleDto?> CreateRoleAsync(string name)
    {
        var response = await http.PostAsJsonAsync("api/v1/admin/roles", new { name });
        if (!response.IsSuccessStatusCode) return null;
        var wrapped = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<RoleDto>>();
        return wrapped?.Data;
    }

    public async Task<List<PermissionDto>> GetPermissionsAsync()
    {
        var response = await http.GetAsync("api/v1/admin/permissions");
        if (!response.IsSuccessStatusCode) return [];
        var wrapped = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<List<PermissionDto>>>();
        return wrapped?.Data ?? [];
    }

    public async Task<PermissionDto?> CreatePermissionAsync(string name, string description)
    {
        var response = await http.PostAsJsonAsync("api/v1/admin/permissions", new { name, description });
        if (!response.IsSuccessStatusCode) return null;
        var wrapped = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<PermissionDto>>();
        return wrapped?.Data;
    }

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        var response = await http.PostAsJsonAsync($"api/v1/admin/roles/{roleId}/permissions", new { permissionId });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AssignRoleToUserAsync(Guid userId, string roleName)
    {
        var response = await http.PostAsJsonAsync($"api/v1/admin/users/{userId}/roles", new { roleName });
        return response.IsSuccessStatusCode;
    }
}

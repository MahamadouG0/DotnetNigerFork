// Contrat applicatif Identity: IRoleService
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;

namespace DotnetNiger.Identity.Application.Services.Interfaces;

// Contrat pour la gestion des roles.
public interface IRoleService
{
	Task<IReadOnlyList<RoleDto>> GetAllAsync();
	Task<RoleDto> CreateAsync(AddRoleRequest request);
	Task DeleteAsync(Guid roleId);
	Task AssignToUserAsync(AssignRoleRequest request);
	Task RemoveFromUserAsync(AssignRoleRequest request);
	Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId);
}

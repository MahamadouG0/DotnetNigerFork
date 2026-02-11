// Contrat applicatif Identity: IPermissionService
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.DTOs.Responses;

namespace DotnetNiger.Identity.Application.Services.Interfaces;

// Contrat pour la gestion des permissions.
public interface IPermissionService
{
	Task<IReadOnlyList<PermissionDto>> GetAllAsync();
	Task<PermissionDto> CreateAsync(AddPermissionRequest request);
	Task DeleteAsync(Guid permissionId);
	Task AssignToRoleAsync(AssignPermissionRequest request);
	Task RemoveFromRoleAsync(AssignPermissionRequest request);
	Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(Guid roleId);
}

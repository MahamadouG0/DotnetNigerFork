// Contrat applicatif Identity: IAdminService
using DotnetNiger.Identity.Application.DTOs.Responses;

namespace DotnetNiger.Identity.Application.Services.Interfaces;

// Contrat pour les operations d'administration.
public interface IAdminService
{
	Task<PaginatedDto<UserSummaryDto>> GetUsersAsync(
		string? search,
		bool? isActive,
		bool? emailConfirmed,
		string? role,
		DateTime? createdFrom,
		DateTime? createdTo,
		string? sortBy,
		string? sortDirection,
		int skip,
		int take);
	Task SetUserActiveAsync(Guid userId, bool isActive);
	Task<PaginatedDto<ApiKeyAuditDto>> GetApiKeysAsync(
		string? search,
		Guid? userId,
		bool? isActive,
		bool? expired,
		DateTime? createdFrom,
		DateTime? createdTo,
		DateTime? lastUsedFrom,
		DateTime? lastUsedTo,
		string? sortBy,
		string? sortDirection,
		int skip,
		int take);
	Task<ApiKeySecretDto> RotateApiKeyAsync(Guid apiKeyId);
	Task RevokeApiKeyAsync(Guid apiKeyId);
	Task RevokeUserApiKeysAsync(Guid userId);
	Task<PaginatedDto<AdminAuditLogDto>> GetAdminAuditLogsAsync(
		Guid? adminUserId,
		string? action,
		string? targetType,
		DateTime? createdFrom,
		DateTime? createdTo,
		int skip,
		int take);
}

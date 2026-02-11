// Contrat applicatif Identity: ILoginHistoryService
using DotnetNiger.Identity.Application.DTOs.Responses;

namespace DotnetNiger.Identity.Application.Services.Interfaces;

// Contrat pour la journalisation des connexions.
public interface ILoginHistoryService
{
	Task RecordAsync(Guid userId, bool success, string? failureReason);
	Task<PaginatedDto<LoginHistoryDto>> GetUserHistoryAsync(Guid userId, int skip, int take);
}

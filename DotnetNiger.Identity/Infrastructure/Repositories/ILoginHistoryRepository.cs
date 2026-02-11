// Repository Identity: ILoginHistoryRepository
using DotnetNiger.Identity.Domain.Entities;

namespace DotnetNiger.Identity.Infrastructure.Repositories;

// Contrat de repository pour l'historique des connexions.
public interface ILoginHistoryRepository : IRepository<LoginHistory>
{
	Task<IReadOnlyList<LoginHistory>> GetForUserAsync(Guid userId, int skip, int take);
}

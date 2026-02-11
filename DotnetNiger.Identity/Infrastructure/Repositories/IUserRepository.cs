// Repository Identity: IUserRepository
using DotnetNiger.Identity.Domain.Entities;

namespace DotnetNiger.Identity.Infrastructure.Repositories;

// Contrat de repository pour les utilisateurs.
public interface IUserRepository : IRepository<ApplicationUser>
{
	Task<ApplicationUser?> GetByEmailAsync(string email);
	Task<ApplicationUser?> GetByUsernameAsync(string username);
}

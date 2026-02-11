// Repository Identity: IRefreshTokenRepository
using DotnetNiger.Identity.Domain.Entities;

namespace DotnetNiger.Identity.Infrastructure.Repositories;

// Contrat de repository pour les refresh tokens.
public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
	Task<RefreshToken?> GetByTokenAsync(string token);
	Task RevokeAsync(RefreshToken refreshToken);
}

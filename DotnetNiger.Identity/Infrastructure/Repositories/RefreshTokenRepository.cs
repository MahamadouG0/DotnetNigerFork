// Repository Identity: RefreshTokenRepository
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Identity.Infrastructure.Repositories;

// Repository pour les refresh tokens.
public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
	public RefreshTokenRepository(DotnetNigerIdentityDbContext dbContext)
		: base(dbContext)
	{
	}

	public Task<RefreshToken?> GetByTokenAsync(string token)
	{
		return DbContext.RefreshTokens.FirstOrDefaultAsync(item => item.Token == token);
	}

	public async Task RevokeAsync(RefreshToken refreshToken)
	{
		refreshToken.RevokedAt = DateTime.UtcNow;
		DbContext.RefreshTokens.Update(refreshToken);
		await DbContext.SaveChangesAsync();
	}
}

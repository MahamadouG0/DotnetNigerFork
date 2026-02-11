// Repository Identity: LoginHistoryRepository
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Identity.Infrastructure.Repositories;

// Repository pour l'historique des connexions.
public class LoginHistoryRepository : BaseRepository<LoginHistory>, ILoginHistoryRepository
{
	public LoginHistoryRepository(DotnetNigerIdentityDbContext dbContext)
		: base(dbContext)
	{
	}

	public async Task<IReadOnlyList<LoginHistory>> GetForUserAsync(Guid userId, int skip, int take)
	{
		return await DbContext.LoginHistories
			.AsNoTracking()
			.Where(item => item.UserId == userId)
			.OrderByDescending(item => item.LoginAt)
			.Skip(skip)
			.Take(take)
			.ToListAsync();
	}
}

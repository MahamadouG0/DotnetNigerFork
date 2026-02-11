// Repository Identity: UserRepository
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Identity.Infrastructure.Repositories;

// Repository pour les utilisateurs.
public class UserRepository : BaseRepository<ApplicationUser>, IUserRepository
{
	public UserRepository(DotnetNigerIdentityDbContext dbContext)
		: base(dbContext)
	{
	}

	public Task<ApplicationUser?> GetByEmailAsync(string email)
	{
		return DbContext.Users.FirstOrDefaultAsync(user => user.Email == email);
	}

	public Task<ApplicationUser?> GetByUsernameAsync(string username)
	{
		return DbContext.Users.FirstOrDefaultAsync(user => user.UserName == username);
	}
}

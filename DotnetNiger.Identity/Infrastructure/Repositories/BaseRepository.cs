// Repository Identity: BaseRepository
using DotnetNiger.Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Identity.Infrastructure.Repositories;

// Repository generique base sur EF Core.
public class BaseRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
	protected readonly DotnetNigerIdentityDbContext DbContext;
	protected readonly DbSet<TEntity> Set;

	public BaseRepository(DotnetNigerIdentityDbContext dbContext)
	{
		DbContext = dbContext;
		Set = dbContext.Set<TEntity>();
	}

	public Task<TEntity?> GetByIdAsync(Guid id)
	{
		return Set.FindAsync(id).AsTask();
	}

	public IQueryable<TEntity> Query()
	{
		return Set.AsQueryable();
	}

	public async Task AddAsync(TEntity entity)
	{
		Set.Add(entity);
		await DbContext.SaveChangesAsync();
	}

	public async Task UpdateAsync(TEntity entity)
	{
		Set.Update(entity);
		await DbContext.SaveChangesAsync();
	}

	public async Task DeleteAsync(TEntity entity)
	{
		Set.Remove(entity);
		await DbContext.SaveChangesAsync();
	}
}

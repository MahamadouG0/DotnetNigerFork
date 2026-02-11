// Repository Identity: IRepository
namespace DotnetNiger.Identity.Infrastructure.Repositories;

// Contrat generique de repository.
public interface IRepository<TEntity> where TEntity : class
{
	Task<TEntity?> GetByIdAsync(Guid id);
	IQueryable<TEntity> Query();
	Task AddAsync(TEntity entity);
	Task UpdateAsync(TEntity entity);
	Task DeleteAsync(TEntity entity);
}

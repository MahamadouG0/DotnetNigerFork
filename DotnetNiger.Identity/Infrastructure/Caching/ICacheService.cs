// Cache Identity: ICacheService
namespace DotnetNiger.Identity.Infrastructure.Caching;

// Contrat de cache distribue.
public interface ICacheService
{
	Task<T?> GetAsync<T>(string key);
	Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
	Task RemoveAsync(string key);
}

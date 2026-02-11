// Cache Identity: RedisCacheService
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace DotnetNiger.Identity.Infrastructure.Caching;

// Cache distribue base sur IDistributedCache.
public class RedisCacheService : ICacheService
{
	private readonly IDistributedCache _cache;

	public RedisCacheService(IDistributedCache cache)
	{
		_cache = cache;
	}

	public async Task<T?> GetAsync<T>(string key)
	{
		var raw = await _cache.GetStringAsync(key);
		if (string.IsNullOrWhiteSpace(raw))
		{
			return default;
		}

		return JsonSerializer.Deserialize<T>(raw);
	}

	public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
	{
		var raw = JsonSerializer.Serialize(value);
		var options = new DistributedCacheEntryOptions();
		if (ttl.HasValue)
		{
			options.SetAbsoluteExpiration(ttl.Value);
		}

		await _cache.SetStringAsync(key, raw, options);
	}

	public Task RemoveAsync(string key)
	{
		return _cache.RemoveAsync(key);
	}
}

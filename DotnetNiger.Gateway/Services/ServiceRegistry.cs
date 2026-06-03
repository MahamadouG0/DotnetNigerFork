using System.Collections.Concurrent;
using DotnetNiger.Gateway.Configuration;

namespace DotnetNiger.Gateway.Services;

public sealed class ServiceRegistration
{
    public required string Id { get; init; }
    public required string Url { get; init; }
    public string? Name { get; init; }
    public string? HealthEndpoint { get; init; }
    public string? SwaggerEndpoint { get; init; }
    public string? ContainerName { get; init; }
    public int? Port { get; init; }
    public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastHeartbeat { get; set; }
}

public interface IServiceRegistry
{
    void RegisterOrUpdate(ServiceRegistration registration);
    bool Unregister(string serviceId);
    ServiceRegistration? Get(string serviceId);
    IReadOnlyList<ServiceRegistration> GetAll();
    IReadOnlyList<DownstreamServiceConfig> GetCombinedConfig();
}

public sealed class ServiceRegistry : IServiceRegistry
{
    private readonly ConcurrentDictionary<string, ServiceRegistration> _dynamic = new();
    private readonly IReadOnlyDictionary<string, DownstreamServiceConfig> _seed;

    public ServiceRegistry(IEnumerable<DownstreamServiceConfig> seedServices)
    {
        _seed = seedServices.ToDictionary(s => s.Id);

        foreach (var seed in seedServices)
        {
            _dynamic.TryAdd(seed.Id, new ServiceRegistration
            {
                Id = seed.Id,
                Url = seed.DevUrl,
                Name = seed.SwaggerName,
                HealthEndpoint = seed.HealthEndpoint,
                SwaggerEndpoint = seed.SwaggerEndpoint,
                ContainerName = seed.ContainerName,
                Port = seed.Port
            });
        }
    }

    public void RegisterOrUpdate(ServiceRegistration registration)
    {
        _dynamic.AddOrUpdate(registration.Id, registration, (_, existing) =>
        {
            return new ServiceRegistration
            {
                Id = registration.Id,
                Url = registration.Url ?? existing.Url,
                Name = registration.Name ?? existing.Name,
                HealthEndpoint = registration.HealthEndpoint ?? existing.HealthEndpoint,
                SwaggerEndpoint = registration.SwaggerEndpoint ?? existing.SwaggerEndpoint,
                ContainerName = registration.ContainerName ?? existing.ContainerName,
                Port = registration.Port ?? existing.Port,
                RegisteredAt = existing.RegisteredAt,
                LastHeartbeat = DateTime.UtcNow
            };
        });
    }

    public bool Unregister(string serviceId) => _dynamic.TryRemove(serviceId, out _);

    public ServiceRegistration? Get(string serviceId) =>
        _dynamic.TryGetValue(serviceId, out var reg) ? reg : null;

    public IReadOnlyList<ServiceRegistration> GetAll() => _dynamic.Values.ToList();

    public IReadOnlyList<DownstreamServiceConfig> GetCombinedConfig()
    {
        var result = _seed.Values.Select(seed =>
        {
            if (_dynamic.TryGetValue(seed.Id, out var reg))
            {
                return new DownstreamServiceConfig
                {
                    Id = seed.Id,
                    ContainerName = reg.ContainerName ?? seed.ContainerName,
                    Port = reg.Port ?? seed.Port,
                    DevUrl = reg.Url,
                    HealthEndpoint = reg.HealthEndpoint ?? seed.HealthEndpoint,
                    SwaggerEndpoint = reg.SwaggerEndpoint ?? seed.SwaggerEndpoint,
                    SwaggerName = seed.SwaggerName,
                    RoutesConfig = seed.RoutesConfig
                };
            }
            return seed;
        }).ToList();

        foreach (var (id, reg) in _dynamic)
        {
            if (!_seed.ContainsKey(id))
            {
                result.Add(new DownstreamServiceConfig
                {
                    Id = reg.Id,
                    ContainerName = reg.ContainerName ?? reg.Id,
                    Port = reg.Port ?? 8080,
                    DevUrl = reg.Url,
                    HealthEndpoint = reg.HealthEndpoint ?? "/health",
                    SwaggerEndpoint = reg.SwaggerEndpoint ?? "/swagger/v1/swagger.json",
                    SwaggerName = reg.Name ?? $"{reg.Id} API",
                    RoutesConfig = $"ocelot.{reg.Id}.routes.json"
                });
            }
        }

        return result;
    }
}

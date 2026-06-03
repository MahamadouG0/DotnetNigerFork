using System.Text.Json;
using Serilog;

namespace DotnetNiger.Gateway.Services;

public static class ServiceRegistrationEndpoint
{
    public static IApplicationBuilder MapServiceRegistryEndpoint(this IApplicationBuilder app)
    {
        var apiKey = string.Empty;

        app.Map("/api/service-registry", registryApp =>
        {
            registryApp.Run(async context =>
            {
                if (!context.Request.Path.Equals("/register", StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
                var key = configuration["Gateway:RegistrationKey"] ?? configuration["Jwt:Key"];

                if (!string.IsNullOrWhiteSpace(key) && !key.StartsWith("__"))
                {
                    var providedKey = context.Request.Headers["X-Registration-Key"].FirstOrDefault();
                    if (string.IsNullOrEmpty(providedKey) || providedKey != key)
                    {
                        Log.Warning("Service registration rejected: invalid or missing X-Registration-Key");
                        context.Response.StatusCode = 401;
                        return;
                    }
                }

                ServiceRegistrationRequest? request;
                try
                {
                    request = await JsonSerializer.DeserializeAsync<ServiceRegistrationRequest>(
                        context.Request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("{\"error\":\"Invalid JSON\"}");
                    return;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.Url))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("{\"error\":\"Id and Url are required\"}");
                    return;
                }

                var registry = context.RequestServices.GetRequiredService<IServiceRegistry>();

                var registration = new ServiceRegistration
                {
                    Id = request.Id.ToLowerInvariant(),
                    Url = request.Url.TrimEnd('/'),
                    Name = request.Name,
                    HealthEndpoint = request.HealthEndpoint,
                    SwaggerEndpoint = request.SwaggerEndpoint,
                    ContainerName = request.ContainerName,
                    Port = request.Port
                };

                registry.RegisterOrUpdate(registration);

                Log.Information("Service registered: {Id} @ {Url}", registration.Id, registration.Url);

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(new { status = "registered", serviceId = registration.Id }));
            });
        });

        return app;
    }
}

public sealed class ServiceRegistrationRequest
{
    public string? Id { get; init; }
    public string? Url { get; init; }
    public string? Name { get; init; }
    public string? HealthEndpoint { get; init; }
    public string? SwaggerEndpoint { get; init; }
    public string? ContainerName { get; init; }
    public int? Port { get; init; }
}

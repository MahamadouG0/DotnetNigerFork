using System.Text.Json;
using System.Text.Json.Nodes;

namespace DotnetNiger.Gateway.Configuration;

public sealed class DownstreamServiceConfig
{
    public string Id { get; init; } = string.Empty;
    public string ContainerName { get; init; } = string.Empty;
    public int Port { get; init; }
    public string DevUrl { get; init; } = string.Empty;
    public string HealthEndpoint { get; init; } = string.Empty;
    public string SwaggerEndpoint { get; init; } = string.Empty;
    public string SwaggerName { get; init; } = string.Empty;
    public string RoutesConfig { get; init; } = string.Empty;
}

public static class OcelotConfigurationBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string BuildMergedConfig(
        string contentRootPath,
        bool useContainerHosts,
        IReadOnlyCollection<DownstreamServiceConfig> services)
    {
        var globalPath = Path.Combine(contentRootPath, "ocelot.global.json");
        if (!File.Exists(globalPath))
            throw new FileNotFoundException("Missing ocelot.global.json");

        var globalNode = JsonNode.Parse(File.ReadAllText(globalPath))?.AsObject()
            ?? throw new InvalidOperationException("Invalid JSON in ocelot.global.json");

        var mergedRoutes = new JsonArray();

        foreach (var service in services)
        {
            var routesPath = Path.Combine(contentRootPath, service.RoutesConfig);
            if (!File.Exists(routesPath))
            {
                continue;
            }

            var serviceNode = JsonNode.Parse(File.ReadAllText(routesPath))?.AsObject();
            if (serviceNode?["Routes"] is not JsonArray routes)
                continue;

            foreach (var route in routes)
            {
                if (route != null)
                    mergedRoutes.Add(route.DeepClone());
            }
        }

        var merged = new JsonObject
        {
            ["Routes"] = mergedRoutes,
            ["GlobalConfiguration"] = globalNode["GlobalConfiguration"]?.DeepClone(),
            ["SwaggerEndPoints"] = BuildSwaggerEndPoints(services, useContainerHosts)
        };

        if (useContainerHosts)
        {
            RewriteToContainerHosts(mergedRoutes, services);
        }

        var baseUrl = "http://localhost:5000";
        if (merged["GlobalConfiguration"] is JsonObject gc)
        {
            gc["BaseUrl"] = useContainerHosts ? "http://gateway:5000" : baseUrl;
        }

        var mergedPath = Path.Combine(contentRootPath, "ocelot.json");
        File.WriteAllText(mergedPath, merged.ToJsonString(JsonOptions));

        return "ocelot.json";
    }

    private static JsonArray BuildSwaggerEndPoints(IReadOnlyCollection<DownstreamServiceConfig> services, bool useContainerHosts)
    {
        var endpoints = new JsonArray();
        foreach (var service in services)
        {
            var swaggerUrl = useContainerHosts
                ? $"http://{service.ContainerName}:{service.Port}{service.SwaggerEndpoint}"
                : $"{service.DevUrl.TrimEnd('/')}{service.SwaggerEndpoint}";

            endpoints.Add(new JsonObject
            {
                ["Key"] = service.Id,
                ["Config"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["Name"] = service.SwaggerName,
                        ["Version"] = "v1",
                        ["Url"] = swaggerUrl
                    }
                }
            });
        }
        return endpoints;
    }

    private static void RewriteToContainerHosts(JsonArray routes, IReadOnlyCollection<DownstreamServiceConfig> services)
    {
        foreach (var routeNode in routes.OfType<JsonObject>())
        {
            if (routeNode["DownstreamHostAndPorts"] is not JsonArray hostAndPorts)
                continue;

            foreach (var hp in hostAndPorts.OfType<JsonObject>())
            {
                var host = hp["Host"]?.GetValue<string>();
                var port = hp["Port"]?.GetValue<int>();

                if (host == null || port == null) continue;

                var match = services.FirstOrDefault(s =>
                    s.DevUrl.Contains($":{port}") || s.DevUrl.EndsWith($":{port}"));

                if (match != null)
                {
                    hp["Host"] = match.ContainerName;
                    hp["Port"] = match.Port;
                }
            }
        }
    }

    public static string BuildMergedConfigWithConsul(
        string contentRootPath,
        IReadOnlyCollection<DownstreamServiceConfig> services)
    {
        var globalPath = Path.Combine(contentRootPath, "ocelot.global.json");
        if (!File.Exists(globalPath))
            throw new FileNotFoundException("Missing ocelot.global.json");

        var globalNode = JsonNode.Parse(File.ReadAllText(globalPath))?.AsObject()
            ?? throw new InvalidOperationException("Invalid JSON in ocelot.global.json");

        var mergedRoutes = new JsonArray();

        foreach (var service in services)
        {
            var routesPath = Path.Combine(contentRootPath, service.RoutesConfig);
            if (!File.Exists(routesPath))
                continue;

            var serviceNode = JsonNode.Parse(File.ReadAllText(routesPath))?.AsObject();
            if (serviceNode?["Routes"] is not JsonArray routes)
                continue;

            foreach (var routeNode in routes.OfType<JsonObject>().Select(r => r.DeepClone().AsObject()))
            {
                routeNode.Remove("DownstreamHostAndPorts");
                routeNode["ServiceName"] = service.ContainerName;
                routeNode["UseServiceDiscovery"] = true;

                mergedRoutes.Add(routeNode);
            }
        }

        var merged = new JsonObject
        {
            ["Routes"] = mergedRoutes,
            ["GlobalConfiguration"] = globalNode["GlobalConfiguration"]?.DeepClone(),
            ["SwaggerEndPoints"] = BuildSwaggerEndPoints(services, useContainerHosts: true)
        };

        if (merged["GlobalConfiguration"] is JsonObject gc)
        {
            gc["BaseUrl"] = "http://gateway:5000";
        }

        var mergedPath = Path.Combine(contentRootPath, "ocelot.json");
        File.WriteAllText(mergedPath, merged.ToJsonString(JsonOptions));

        return "ocelot.json";
    }
}

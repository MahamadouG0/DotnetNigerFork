using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Serilog;
using Serilog.Events;

// ─── Serilog bootstrap (avant la création du host) ───────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Ocelot", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Gateway")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/gateway-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
    .CreateLogger();

Log.Information("Démarrage du DotnetNiger API Gateway...");

// ─── Builder ─────────────────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Host.UseSerilog();

var mergedOcelotFile = BuildMergedOcelotConfiguration(
    builder.Environment.ContentRootPath,
    builder.Environment.IsProduction());

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile(mergedOcelotFile, optional: false, reloadOnChange: false);

// ─── Services ────────────────────────────────────────────────────────────────
builder.Services.AddOcelot(builder.Configuration)
    .AddCacheManager(x => x.WithDictionaryHandle())
    .AddPolly();

builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerForOcelot(builder.Configuration);

builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ─── JWT (même clé que Identity, partagée avec Community) ────────────────────
var jwtKey = builder.Configuration["Jwt:Key"];
if (!string.IsNullOrWhiteSpace(jwtKey) && jwtKey.Length >= 32 && !jwtKey.StartsWith("__"))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer("Bearer", options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "DotnetNiger.Identity",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "DotnetNiger.Identity.Client",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    context.Token = authHeader["Bearer ".Length..].Trim();
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var path = context.HttpContext.Request.Path.Value ?? string.Empty;
                var isPublicPath = path.StartsWith("/api/diagnostics", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);

                if (!isPublicPath)
                {
                    Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
                }
                return Task.CompletedTask;
            }
        };
    });

    Log.Information("JWT Authentication configurée (Issuer={Issuer})",
        builder.Configuration["Jwt:Issuer"] ?? "DotnetNiger.Identity");
}
else
{
    Log.Warning("JWT Key non configurée ou invalide — authentification Ocelot désactivée");
}

// ─── App ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseCors("AllowAll");

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    if (path.StartsWith("/metrics/latency", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    await next();
    stopwatch.Stop();

    var route = context.GetEndpoint()?.DisplayName ?? path;
    var key = $"{context.Request.Method} {route}";
    GatewayEndpointLatencyMetrics.Record(key, stopwatch.Elapsed.TotalMilliseconds, context.Response.StatusCode);
});

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "DotnetNiger.Gateway",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/metrics/latency", () => Results.Ok(GatewayEndpointLatencyMetrics.GetSnapshot()))
    .AllowAnonymous();

app.MapGet("/health/downstream", async (IHttpClientFactory factory, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    var identityHealthUrl = configuration["DownstreamServices:Identity:HealthUrl"] ?? "http://localhost:5075/api/v1/diagnostics/health";
    var communityHealthUrl = configuration["DownstreamServices:Community:HealthUrl"] ?? "http://localhost:5269/api/v1/test/health";

    var identityStatus = await CheckDownstreamAsync(factory, identityHealthUrl, cancellationToken);
    var communityStatus = await CheckDownstreamAsync(factory, communityHealthUrl, cancellationToken);

    var allHealthy = identityStatus.IsHealthy && communityStatus.IsHealthy;

    return Results.Json(new
    {
        status = allHealthy ? "Healthy" : "Degraded",
        service = "DotnetNiger.Gateway",
        timestamp = DateTime.UtcNow,
        downstream = new
        {
            identity = identityStatus,
            community = communityStatus
        }
    }, statusCode: allHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
});

app.MapGet("/health/ready", async (IHttpClientFactory factory, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    var identityHealthUrl = configuration["DownstreamServices:Identity:HealthUrl"] ?? "http://localhost:5075/api/v1/diagnostics/health";
    var communityHealthUrl = configuration["DownstreamServices:Community:HealthUrl"] ?? "http://localhost:5269/api/v1/test/health";

    var identityStatus = await CheckDownstreamAsync(factory, identityHealthUrl, cancellationToken);
    var communityStatus = await CheckDownstreamAsync(factory, communityHealthUrl, cancellationToken);
    var allHealthy = identityStatus.IsHealthy && communityStatus.IsHealthy;

    return allHealthy ? Results.Ok() : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.Urls.Add("http://localhost:5000");
}

// Garantit un identifiant client pour le rate limiting Ocelot
app.Use(async (context, next) =>
{
    var resolvedClientId = context.Request.Headers["ClientId"].FirstOrDefault()
                           ?? context.Request.Headers["Oc-Client"].FirstOrDefault()
                           ?? context.Connection.RemoteIpAddress?.MapToIPv4().ToString()
                           ?? "unknown-client";

    if (!context.Request.Headers.ContainsKey("ClientId"))
        context.Request.Headers["ClientId"] = resolvedClientId;

    if (!context.Request.Headers.ContainsKey("Oc-Client"))
        context.Request.Headers["Oc-Client"] = resolvedClientId;

    await next.Invoke();
});

// Propagation et journalisation du X-Request-ID
app.Use(async (context, next) =>
{
    var requestId = context.Request.Headers["X-Request-ID"].FirstOrDefault()
                    ?? Guid.NewGuid().ToString("N");
    context.Response.Headers["X-Request-ID"] = requestId;
    Log.Information("→ {Method} {Path}", context.Request.Method, context.Request.Path);
    await next.Invoke();
    Log.Information("← {StatusCode}", context.Response.StatusCode);
});

// Middleware de fusion — DOIT être AVANT UseSwaggerForOcelotUI pour court-circuiter MMLib
app.Use(async (context, next) =>
{
    try
    {
        var isMergedSwaggerPath = context.Request.Path.Equals("/swagger/docs/v1/all", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.Equals("/swagger/v1/swagger.json", StringComparison.OrdinalIgnoreCase);

        if (!isMergedSwaggerPath)
        {
            await next(context);
            return;
        }

        var factory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        var identitySwaggerUrl = configuration["DownstreamServices:Identity:SwaggerUrl"] ?? "http://localhost:5075/swagger/v1/swagger.json";
        var communitySwaggerUrl = configuration["DownstreamServices:Community:SwaggerUrl"] ?? "http://localhost:5269/swagger/v1/swagger.json";

        // Appels directs vers les services aval (évite les boucles via Ocelot)
        var identityJson = await FetchSwaggerJsonAsync(factory, identitySwaggerUrl, context.RequestAborted);
        var communityJson = await FetchSwaggerJsonAsync(factory, communitySwaggerUrl, context.RequestAborted);

        if (identityJson == null && communityJson == null)
        {
            Log.Warning("Swagger aggregation failed: both downstream swagger documents are unavailable");
            context.Response.StatusCode = 503;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"message\":\"Downstream swagger documents are unavailable\"}");
            return;
        }

        var merged = identityJson ?? communityJson!;
        if (identityJson != null && communityJson != null)
        {
            var idoc = JsonNode.Parse(identityJson)!.AsObject();
            var cdoc = JsonNode.Parse(communityJson)!.AsObject();

            if (idoc["info"] is JsonObject info)
                info["title"] = "DotnetNiger - All APIs";

            var paths = idoc["paths"]?.AsObject() ?? new JsonObject();
            if (cdoc["paths"] is JsonObject cPaths)
                foreach (var p in cPaths)
                    paths[p.Key] = p.Value?.DeepClone();
            idoc["paths"] = paths;

            var iComponents = idoc["components"]?.AsObject() ?? new JsonObject();
            var iSchemas = iComponents["schemas"]?.AsObject() ?? new JsonObject();
            if (cdoc["components"]?["schemas"] is JsonObject cSchemas)
                foreach (var s in cSchemas)
                    if (!iSchemas.ContainsKey(s.Key))
                        iSchemas[s.Key] = s.Value?.DeepClone();
            iComponents["schemas"] = iSchemas;
            idoc["components"] = iComponents;
            merged = idoc.ToJsonString();
        }



        var gatewayServer = $"{context.Request.Scheme}://{context.Request.Host}";
        var normalized = JsonNode.Parse(merged)?.AsObject();
        if (normalized != null)
        {
            normalized["servers"] = new JsonArray
        {
            new JsonObject
            {
                ["url"] = gatewayServer
            }
        };

            merged = normalized.ToJsonString();
        }

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(merged);

    }
    catch (Exception ex)
    {
        Console.WriteLine("Erreur : " + ex.Message);
    }


});

app.UseSwaggerForOcelotUI(
    opt =>
    {
        opt.PathToSwaggerGenerator = "/swagger/docs";
    },
    uiOpt =>
    {
        uiOpt.EnableFilter();
        uiOpt.EnableDeepLinking();
        uiOpt.DisplayRequestDuration();
        uiOpt.EnablePersistAuthorization();
        uiOpt.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });

await app.UseOcelot();
await app.RunAsync();

static string BuildMergedOcelotConfiguration(string contentRootPath, bool useContainerHosts)
{
    var globalPath = Path.Combine(contentRootPath, "ocelot.global.json");
    var identityPath = Path.Combine(contentRootPath, "ocelot.identity.routes.json");
    var communityPath = Path.Combine(contentRootPath, "ocelot.community.routes.json");

    if (!File.Exists(globalPath) || !File.Exists(identityPath) || !File.Exists(communityPath))
    {
        throw new FileNotFoundException("One or more split Ocelot config files are missing.");
    }

    var globalNode = JsonNode.Parse(File.ReadAllText(globalPath))?.AsObject()
        ?? throw new InvalidOperationException("Invalid JSON in ocelot.global.json");
    var identityNode = JsonNode.Parse(File.ReadAllText(identityPath))?.AsObject()
        ?? throw new InvalidOperationException("Invalid JSON in ocelot.identity.routes.json");
    var communityNode = JsonNode.Parse(File.ReadAllText(communityPath))?.AsObject()
        ?? throw new InvalidOperationException("Invalid JSON in ocelot.community.routes.json");

    var mergedRoutes = new JsonArray();
    AppendRoutes(mergedRoutes, identityNode, "ocelot.identity.routes.json");
    AppendRoutes(mergedRoutes, communityNode, "ocelot.community.routes.json");

    var merged = new JsonObject
    {
        ["Routes"] = mergedRoutes,
        ["GlobalConfiguration"] = globalNode["GlobalConfiguration"]?.DeepClone(),
        ["SwaggerEndPoints"] = globalNode["SwaggerEndPoints"]?.DeepClone()
    };

    if (useContainerHosts)
    {
        RewriteDownstreamTargets(mergedRoutes, "localhost", 5075, "identity", 8081);
        RewriteDownstreamTargets(mergedRoutes, "localhost", 5269, "community", 8082);
        RewriteSwaggerTargets(merged);
    }

    var mergedPath = Path.Combine(contentRootPath, "ocelot.json");
    File.WriteAllText(mergedPath, merged.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

    return "ocelot.json";
}

static void AppendRoutes(JsonArray target, JsonObject source, string fileName)
{
    if (source["Routes"] is not JsonArray routes)
    {
        throw new InvalidOperationException($"Missing Routes array in {fileName}");
    }

    foreach (var route in routes)
    {
        if (route != null)
        {
            target.Add(route.DeepClone());
        }
    }
}

static void RewriteDownstreamTargets(JsonArray routes, string fromHost, int fromPort, string toHost, int toPort)
{
    foreach (var routeNode in routes.OfType<JsonObject>())
    {
        if (routeNode["DownstreamHostAndPorts"] is not JsonArray hostAndPorts)
        {
            continue;
        }

        foreach (var hostPortNode in hostAndPorts.OfType<JsonObject>())
        {
            var host = hostPortNode["Host"]?.GetValue<string>();
            var port = hostPortNode["Port"]?.GetValue<int?>();

            if (string.Equals(host, fromHost, StringComparison.OrdinalIgnoreCase) && port == fromPort)
            {
                hostPortNode["Host"] = toHost;
                hostPortNode["Port"] = toPort;
            }
        }
    }
}

static void RewriteSwaggerTargets(JsonObject merged)
{
    if (merged["SwaggerEndPoints"] is not JsonArray swaggerEndPoints)
    {
        return;
    }

    foreach (var swaggerEndpoint in swaggerEndPoints.OfType<JsonObject>())
    {
        if (swaggerEndpoint["Key"]?.GetValue<string>() == "identity" && swaggerEndpoint["Config"] is JsonArray identityConfigs)
        {
            foreach (var config in identityConfigs.OfType<JsonObject>())
            {
                config["Url"] = "http://identity:8081/swagger/v1/swagger.json";
            }
        }

        if (swaggerEndpoint["Key"]?.GetValue<string>() == "community" && swaggerEndpoint["Config"] is JsonArray communityConfigs)
        {
            foreach (var config in communityConfigs.OfType<JsonObject>())
            {
                config["Url"] = "http://community:8082/swagger/v1/swagger.json";
            }
        }
    }
}

static async Task<string?> FetchSwaggerJsonAsync(IHttpClientFactory factory, string url, CancellationToken cancellationToken)
{
    try
    {
        using var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(4);
        return await client.GetStringAsync(url, cancellationToken);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to fetch swagger document from {Url}", url);
        return null;
    }
}

static async Task<DownstreamHealthStatus> CheckDownstreamAsync(IHttpClientFactory factory, string url, CancellationToken cancellationToken)
{
    try
    {
        using var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(3);
        using var response = await client.GetAsync(url, cancellationToken);

        return new DownstreamHealthStatus(
            url,
            response.IsSuccessStatusCode,
            (int)response.StatusCode,
            response.ReasonPhrase);
    }
    catch (Exception ex)
    {
        return new DownstreamHealthStatus(
            url,
            false,
            (int)HttpStatusCode.ServiceUnavailable,
            ex.Message);
    }
}

readonly record struct DownstreamHealthStatus(string Url, bool IsHealthy, int StatusCode, string? Reason);

static class GatewayEndpointLatencyMetrics
{
    private const int MaxSamples = 2048;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, EndpointLatencyWindow> Windows = new(StringComparer.OrdinalIgnoreCase);

    public static void Record(string endpointKey, double elapsedMs, int statusCode)
    {
        var window = Windows.GetOrAdd(endpointKey, _ => new EndpointLatencyWindow(MaxSamples));
        window.Record(elapsedMs, statusCode);
    }

    public static object GetSnapshot(int top = 5)
    {
        var endpoints = Windows
            .Select(item => new
            {
                endpoint = item.Key,
                metrics = item.Value.Snapshot()
            })
            .Where(item => item.metrics.Count > 0)
            .OrderByDescending(item => item.metrics.P95Ms)
            .Take(Math.Max(1, top))
            .ToList();

        return new
        {
            generatedAtUtc = DateTime.UtcNow,
            endpointCount = Windows.Count,
            topSlowest = endpoints
        };
    }

    private sealed class EndpointLatencyWindow
    {
        private readonly int _maxSamples;
        private readonly object _lock = new();
        private readonly Queue<double> _latencies = new();
        private long _requestCount;
        private long _errorCount;

        public EndpointLatencyWindow(int maxSamples)
        {
            _maxSamples = maxSamples;
        }

        public void Record(double elapsedMs, int statusCode)
        {
            lock (_lock)
            {
                _requestCount++;
                if (statusCode >= 500)
                {
                    _errorCount++;
                }

                _latencies.Enqueue(elapsedMs);
                while (_latencies.Count > _maxSamples)
                {
                    _latencies.Dequeue();
                }
            }
        }

        public EndpointSnapshot Snapshot()
        {
            lock (_lock)
            {
                if (_latencies.Count == 0)
                {
                    return EndpointSnapshot.Empty;
                }

                var values = _latencies.OrderBy(value => value).ToArray();
                return new EndpointSnapshot
                {
                    Count = values.Length,
                    TotalRequests = _requestCount,
                    TotalErrors = _errorCount,
                    P50Ms = Percentile(values, 0.50),
                    P95Ms = Percentile(values, 0.95),
                    P99Ms = Percentile(values, 0.99)
                };
            }
        }

        private static double Percentile(double[] sortedValues, double percentile)
        {
            if (sortedValues.Length == 0)
            {
                return 0;
            }

            var index = (int)Math.Ceiling(percentile * sortedValues.Length) - 1;
            index = Math.Clamp(index, 0, sortedValues.Length - 1);
            return Math.Round(sortedValues[index], 2);
        }
    }

    private sealed class EndpointSnapshot
    {
        public static EndpointSnapshot Empty { get; } = new();

        public int Count { get; init; }
        public long TotalRequests { get; init; }
        public long TotalErrors { get; init; }
        public double P50Ms { get; init; }
        public double P95Ms { get; init; }
        public double P99Ms { get; init; }
    }
}




# DotnetNiger.Gateway

Ocelot API Gateway — the single entry point for the DotnetNiger platform. Routes requests to downstream services (Identity, Community, and future services) with authentication, rate limiting, QoS, caching, and Swagger aggregation.

## Tech Stack

- .NET 9.0
- Ocelot 24.x (routing, rate limiting, QoS, caching)
- Ocelot.Cache.CacheManager (response caching)
- Ocelot.Provider.Polly (circuit breaker)
- MMLib.SwaggerForOcelot (aggregated Swagger UI)
- JWT Bearer authentication
- Serilog (structured logging)

## Architecture

```
Client → Gateway (:5000) → Identity (:5075 / :8081)
                        → Community (:5269 / :8082)
                        → Future services (...)
```

## Project Structure

```
DotnetNiger.Gateway/
├── Program.cs                              # Entry point
├── DotnetNiger.Gateway.csproj              # .NET 9.0
├── Dockerfile                              # aspnet:9.0
├── appsettings.json                        # Multi-service configuration
├── appsettings.Development.json
├── Properties/launchSettings.json
├── ocelot.global.json                      # Global Ocelot config
├── ocelot.identity.routes.json             # Identity route definitions
├── ocelot.community.routes.json            # Community route definitions
├── Configuration/
│   └── OcelotConfigurationBuilder.cs       # Dynamic route file merging
├── Extensions/
│   ├── ServiceCollectionExtensions.cs      # DI registration + JWT config
│   └── ApplicationBuilderExtensions.cs     # Middleware pipeline + health endpoints
├── Metrics/
│   └── EndpointLatencyMetrics.cs           # Per-endpoint latency tracking
└── Services/
    ├── ServiceRegistry.cs                  # IServiceRegistry + ServiceRegistry singleton
    └── ServiceRegistrationEndpoint.cs      # POST /api/service-registry/register handler
```

## Service Discovery

The Gateway uses a **two-tier service discovery** system:

### 1. Static Seed Config (`appsettings.json`)

`DownstreamServices` provides initial entries and Ocelot route definitions:

```json
{
  "DownstreamServices": {
    "Identity": { "Id": "identity", "DevUrl": "http://localhost:5075", ... },
    "Community": { "Id": "community", "DevUrl": "http://localhost:5269", ... }
  }
}
```

### 2. Dynamic ServiceRegistry (In-Memory)

`ServiceRegistry` singleton (`Services/ServiceRegistry.cs`):
- Thread-safe `ConcurrentDictionary` keyed by service `Id`
- Seeded from `DownstreamServices` at startup
- Accepts runtime registrations via `POST /api/service-registry/register`
- `GetCombinedConfig()` returns merged view (static + dynamic)

### Self-Registration

Services can register dynamically at startup:

```http
POST /api/service-registry/register
Content-Type: application/json
X-Registration-Key: <optional-key>

{ "id": "my-service", "url": "http://my-service:8080", "healthEndpoint": "/health" }
```

Registration auth is optional — configure `Gateway:RegistrationKey` in `appsettings.json` or User Secrets.

Both Identity and Community automatically self-register on startup. Logs on success/failure are non-fatal.

## Key Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /swagger` | Aggregated Swagger UI (all downstream services) |
| `GET /health` | Gateway liveness |
| `GET /health/ready` | Readiness (all downstreams must respond) |
| `GET /health/downstream` | Detailed health of each downstream service |
| `GET /health/services` | Registered services configuration (static + dynamic) |
| `POST /api/service-registry/register` | Dynamic service registration |
| `GET /metrics/latency` | Endpoint latency statistics (P50/P95/P99) |

## Development

```bash
cd DotnetNiger.Gateway
dotnet run
```

The merged Ocelot configuration (`ocelot.json`) is auto-generated at startup from the split route files and is excluded from version control (`.gitignore`).

## Docker

```bash
# From repository root
docker build -f DotnetNiger.Gateway/Dockerfile -t dotnetniger-gateway .
docker run -p 5000:5000 dotnetniger-gateway
```

Or via docker-compose:

```bash
docker-compose up gateway
```

## Middleware Pipeline (in order)

1. **CORS** — AllowAll policy
2. **Latency Metrics** — Record request duration per endpoint
3. **Client ID Resolution** — Rate limiting client identification
4. **Request Tracing** — X-Request-ID header propagation + logging
5. **Swagger Merge** — Custom aggregation of downstream Swagger docs (uses `IServiceRegistry.GetCombinedConfig()`)
6. **Health Endpoints** — Map `/health/*` routes (uses `IServiceRegistry.GetCombinedConfig()`)
7. **Service Registry** — Map `/api/service-registry/register` endpoint
8. **SwaggerForOcelot UI** — Aggregated Swagger interface
9. **Ocelot** — Route matching, authentication, forwarding

# Architecture

## Overview

DotnetNiger follows a **microservices architecture** with an **API Gateway** as the single entry point. All client traffic flows through the Gateway, which handles routing, authentication, rate limiting, and Swagger aggregation.

```
                    ┌─────────────┐
                    │   Client    │
                    │ (Browser /  │
                    │  Mobile /   │
                    │  API)       │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │   Gateway   │  Port 5000
                    │  (Ocelot)   │
                    │  .NET 9.0   │
                    └──────┬──────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
       ┌──────▼──────┐ ┌──▼──────┐ ┌──▼──────────┐
       │  Identity   │ │Community│ │  Future      │
       │  :5075      │ │ :5269   │ │  Services    │
       │  (:8081)    │ │ (:8082) │ │  (...)       │
       │  OpenIddict │ │ JWT     │ │              │
       │  OIDC       │ │ Bearer  │ │              │
       └──────┬──────┘ └─────────┘ └─────────────┘
              │
       ┌──────▼──────┐
       │  SQLite     │
       │  (EF Core)  │
       └─────────────┘
```

## Service Responsibilities

### Gateway (`DotnetNiger.Gateway`)

- **Single entry point** — all external requests go through port `5000`
- **Routing** — Ocelot maps upstream paths to downstream services
- **Authentication** — Validates JWT tokens before forwarding to downstream services
- **Rate Limiting** — Per-route rate limits (configurable in route JSON files)
- **QoS (Quality of Service)** — Circuit breaker via Polly, request timeouts
- **Caching** — Response caching for read-heavy endpoints (posts, events, resources)
- **Swagger Aggregation** — Merges all downstream Swagger docs into a single UI
- **Health Checks** — `/health`, `/health/downstream`, `/health/ready`, `/health/services`
- **Latency Metrics** — `/metrics/latency` endpoint with P50/P95/P99 percentiles
- **Request Tracing** — `X-Request-ID` header propagation and logging

### Identity (`DotnetNiger.Identity`)

- **Authentication** — Register, login, email confirmation, social login (Google, Microsoft, GitHub)
- **OAuth2 / OIDC** — OpenIddict-based token endpoint (`/connect/token`) with password and refresh token grants
- **Multi-Tenancy** — Tenant isolation via `TenantId` claim in JWT
- **User Management** — CRUD users, roles, permissions, API keys
- **Admin** — Super-admin bootstrap, platform stats, tenant management
- **Profile** — Get/update/delete own profile

### Community (`DotnetNiger.Community`)

- **Posts** — CRUD with pagination, searchable
- **Events** — CRUD with registration system, publish/unpublish workflow
- **Resources** — CRUD with view counting, categorized
- **Comments** — Nested comments on posts and events
- **Newsletters** — Subscribe, verify, unsubscribe with admin management
- **Member Profiles** — Profile, social links within the community
- **Admin** — Dashboard stats, user/role/permission management, event moderation
- **Search** — Full-text search across posts, events, and resources
- **Tags & Categories** — Content classification
- **Projects & Partners** — Community directory

## Dependency Rules

The key architectural constraint enforced by tests is:

> **Application layer must NOT reference Infrastructure.Repositories directly**

### Enforcement

Architecture tests are in `DotnetNiger.Tests/DotnetNiger.Architecture.Tests/` and verify:
- Application projects only depend on Domain and Abstractions
- Infrastructure implements abstractions defined in Application
- No circular dependencies between projects

### Clean Architecture Layer Structure

```
Api/Controllers/        → REST endpoints
Api/Middleware/         → Error handling, custom middleware
Api/ServiceExtensions   → DI registration extension methods
Application/DTOs/       → Request/Response data transfer objects
Application/Services/   → Business logic
Domain/Entities/        → EF Core entities
Infrastructure/         → DbContext, repositories, seeding
```

## Communication Between Services

- **Gateway → all**: HTTP routing via Ocelot configuration
- **Community → Identity**: Typed HTTP client (`IIdentityApiClient`) for user/role provisioning
- **Identity → Community**: Inter-service calls for user data synchronization
- **All services → Gateway**: Self-registration via `POST /api/service-registry/register` on startup

## Dynamic Service Discovery

The Gateway uses a **two-tier service discovery** system: static config seeds initial entries, and a dynamic `ServiceRegistry` accepts runtime registration and heartbeat updates.

### Architecture

```
                    ┌─────────────────┐
                    │    Gateway      │
                    │  ServiceRegistry│
                    │  (ConcurrentDict)│
                    └──────┬──────────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
     ┌────────▼────┐ ┌────▼────┐ ┌─────▼──────┐
     │  Identity   │ │Community│ │  Future     │
     │ (self-reg.) │ │ (self-) │ │  (register  │
     │     POST    │ │  POST   │ │   via API)  │
     └─────────────┘ └─────────┘ └────────────┘
```

### ServiceRegistry (`DotnetNiger.Gateway/Services/ServiceRegistry.cs`)

- `IServiceRegistry` singleton — thread-safe, backed by `ConcurrentDictionary`
- Seeded from `appsettings.json` → `DownstreamServices` at startup
- `RegisterOrUpdate()` merges dynamic registrations over static seeds
- `GetCombinedConfig()` returns merged view (static + dynamic)
- Dynamic registrations **not** persisted to `appsettings.json` (in-memory only)

### Registration Endpoint

```http
POST /api/service-registry/register
Content-Type: application/json
X-Registration-Key: <optional-api-key>

{
  "id": "my-service",
  "url": "http://my-service:8080",
  "name": "My Service API",
  "healthEndpoint": "/health",
  "swaggerEndpoint": "/swagger/v1/swagger.json",
  "containerName": "my-service",
  "port": 8080
}
```

| Header | Required | Description |
|--------|----------|-------------|
| `X-Registration-Key` | If `Gateway:RegistrationKey` is set | Must match the configured key |

- Auth is optional: if `Gateway:RegistrationKey` is empty or starts with `__`, registration is open
- `id` is lowercased and used as the service identifier in health checks and service listing
- `url` is trimmed of trailing `/` and used as the base URL for health checks and swagger fetching

### Self-Registration at Startup

Both Identity and Community auto-register with the Gateway at startup:

| Service | Config Source | URL Used |
|---------|-------------|----------|
| Identity | `Smtp:AppBaseUrl` (default `http://localhost:5075`) | Base for health token |
| Community | `Jwt:Authority` (default `http://localhost:5269`) | Base for health token |

```json
{
  "Gateway": {
    "RegistrationUrl": "http://localhost:5000/api/service-registry/register",
    "RegistrationKey": "__SET_VIA_ENV_OR_USER_SECRETS__"
  }
}
```

Self-registration is non-fatal: if the Gateway is unavailable at startup, the service logs a warning and continues.

### Static Config (Seed)

The static `DownstreamServices` config still serves two purposes:
1. Provides **route definitions** (`ocelot.<service>.routes.json`) for Ocelot at startup
2. Provides **initial seed** entries for the `ServiceRegistry`

```json
{
  "DownstreamServices": {
    "Identity": {
      "Id": "identity",
      "ContainerName": "identity",
      "Port": 8081,
      "DevUrl": "http://localhost:5075",
      "HealthEndpoint": "/api/v1/diagnostics/health",
      "SwaggerEndpoint": "/swagger/v1/swagger.json",
      "SwaggerName": "Identity API",
      "RoutesConfig": "ocelot.identity.routes.json"
    }
  }
}
```

### Adding a New Service

Two options:

**Option A — Static (no code change to Gateway):**
1. Create `ocelot.newservice.routes.json`
2. Add `DownstreamServices:NewService` section in Gateway `appsettings.json`
3. Add container in `docker-compose.yml`
4. The service appears at next Gateway restart

**Option B — Dynamic (self-registration, no Gateway restart):**
1. Create `ocelot.newservice.routes.json`
2. Add seed config in Gateway `appsettings.json` (for Ocelot routes)
3. Service calls `POST /api/service-registry/register` at startup
4. Gateway immediately discovers the service for health checks
5. Ocelot route reload requires Gateway restart (routes are built at startup)

## Health & Observability

| Endpoint | Description |
|----------|-------------|
| `GET /health` | Gateway liveness probe |
| `GET /health/ready` | Readiness probe (all downstreams must respond) |
| `GET /health/downstream` | Detailed health of all downstream services (static + dynamic) |
| `GET /health/services` | Registered service configuration (static + dynamic) |
| `POST /api/service-registry/register` | Dynamic service registration endpoint |
| `GET /metrics/latency` | Per-endpoint latency (P50/P95/P99) |

All health endpoints use `IServiceRegistry.GetCombinedConfig()` — dynamically registered services are checked alongside statically configured ones.

All services use **Serilog** for structured logging (console + rolling file).

## Technology Map

| Area | Technology |
|------|-----------|
| Runtime | .NET 9.0 |
| API Gateway | Ocelot 24.x |
| Auth | ASP.NET Core Identity + OpenIddict 5.x |
| OAuth Providers | Google, Microsoft, GitHub |
| ORM | Entity Framework Core 9.x |
| Database | SQLite (dev) |
| Validation | FluentValidation (Identity) |
| API Docs | Swashbuckle / Swagger |
| Logging | Serilog |
| Resilience | Polly (via Ocelot) |
| Caching | Ocelot CacheManager |
| Container | Docker, docker-compose |
| Email | MailKit (SMTP) |

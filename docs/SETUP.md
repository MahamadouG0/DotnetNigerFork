# Setup Guide

## Prerequisites

- .NET SDK 9.0+
- Git
- Docker + docker-compose (optional, for containerized deployment)

## Clone & Restore

```bash
git clone https://github.com/akaletekoffilevis/DotnetNiger.git
cd DotnetNiger
dotnet restore DotnetNiger.slnx
```

## Local Development (4 Terminals)

Start services in this order: **Identity → Community → Gateway → Identity.Web**.

### Terminal 1 — Identity Service

```bash
cd DotnetNiger.Identity
dotnet run
```

Runs on `http://localhost:5075`. The database is auto-created (SQLite). A super admin is seeded on first run:

- Email: `admin@dotnetniger.com`
- Password: `Admin@123456`

Swagger: `http://localhost:5075/swagger`

### Terminal 2 — Community Service

```bash
cd DotnetNiger.Community
dotnet run
```

Runs on `http://localhost:5269`. Depends on Identity being running for JWT validation.

Swagger: `http://localhost:5269/swagger`

### Terminal 3 — Gateway Service

```bash
cd DotnetNiger.Gateway
dotnet run
```

Runs on `http://localhost:5000`. The Ocelot configuration is auto-generated at startup by merging:

- `ocelot.global.json` (global settings)
- `ocelot.identity.routes.json` (Identity routes)
- `ocelot.community.routes.json` (Community routes)

The Gateway seeds an in-memory `ServiceRegistry` from `DownstreamServices` config. Identity and Community self-register dynamically at startup via `POST /api/service-registry/register`.

Swagger (aggregated): `http://localhost:5000/swagger`

### Terminal 4 — Identity.Web (Developer Portal)

```bash
cd DotnetNiger.Identity.Web
dotnet run
```

Runs on `http://localhost:5100`. Requires Identity Server (port 5075) to be running.

This is the developer portal UI with authentication, dashboard, admin panels, profile, and security pages. It uses the OIDC code flow to authenticate with Identity Server.

Test credentials: `admin@dotnetniger.com` / `Admin@123456`

### Terminal 5 (optional) — TestIdentity

```bash
cd DotnetNiger.TestIdentity
dotnet run
```

Runs on `http://localhost:5200`. A minimal OIDC test app to validate the OpenIddict flow works end-to-end.

## Docker Deployment

```bash
docker-compose up --build
```

This starts all three core services with their container ports:

- Gateway: `localhost:5000`
- Identity: `localhost:8081`
- Community: `localhost:8082`

## Build & Test

```bash
# Build all projects
dotnet build DotnetNiger.slnx

# Run tests
dotnet test DotnetNiger.slnx

# Quick CI-equivalent
dotnet restore DotnetNiger.slnx
dotnet build DotnetNiger.slnx --configuration Release --no-restore
dotnet test DotnetNiger.slnx --configuration Release --no-build
```

## Configuration Variables

### JWT (required — must match between Identity and Gateway)

```json
{
  "Jwt": {
    "Key": "YourSecretKeyMin32CharactersLong!",
    "Issuer": "DotnetNiger.Identity",
    "Audience": "DotnetNiger.Identity.Client"
  }
}
```

### SMTP (optional — for email confirmation)

```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your@email.com",
    "Password": "your-password",
    "FromEmail": "noreply@dotnetniger.com",
    "FromName": "DotnetNiger",
    "AppBaseUrl": "http://localhost:5075"
  }
}
```

If `Host` is empty, emails are logged to console and confirmation codes are returned directly in API responses.

### Social Login (optional)

```json
{
  "Authentication": {
    "Google": { "ClientId": "...", "ClientSecret": "..." },
    "Microsoft": { "ClientId": "...", "ClientSecret": "..." },
    "GitHub": { "ClientId": "...", "ClientSecret": "..." }
  }
}
```

Providers are only activated when their `ClientId` is non-empty.

### User Secrets

Sensitive values must be stored via `dotnet user-secrets` — never in `appsettings.json`.

**Identity:**

```bash
cd DotnetNiger.Identity
dotnet user-secrets set "Smtp:Password" "your-password"
dotnet user-secrets set "Authentication:Google:ClientId" "your-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-secret"
dotnet user-secrets set "Gateway:RegistrationKey" "your-gateway-key"
```

**Gateway:**

```bash
cd DotnetNiger.Gateway
dotnet user-secrets set "Gateway:RegistrationKey" "your-secret-key"
```

**Identity.Web:**

```bash
cd DotnetNiger.Identity.Web
dotnet user-secrets set "Identity:ClientSecret" "web-ui-client-secret"
```

### Service Self-Registration

Each service registers itself with the Gateway at startup via:

```http
POST /api/service-registry/register
Content-Type: application/json

{ "id": "identity", "url": "http://localhost:5075", ... }
```

Config per service (`appsettings.json`):

```json
{
  "Gateway": {
    "RegistrationUrl": "http://localhost:5000/api/service-registry/register",
    "RegistrationKey": "__SET_VIA_ENV_OR_USER_SECRETS__"
  }
}
```

| Variable | Purpose |
|----------|---------|
| `Gateway:RegistrationUrl` | Gateway registration endpoint URL |
| `Gateway:RegistrationKey` | Optional API key sent as `X-Registration-Key` header |

If `RegistrationKey` is empty or starts with `__`, registration is anonymous.

### Adding a New Service

**Option A — Static config + restart:**

1. Add `ocelot.<service>.routes.json` in the Gateway project
2. Add a `DownstreamServices:<Service>` section in `appsettings.json`
3. Add the service container in `docker-compose.yml`
4. Create the service project following Identity/Community patterns

**Option B — Dynamic self-registration (no Gateway restart):**

1. Add `ocelot.<service>.routes.json` in the Gateway project (required for Ocelot route definitions)
2. Add seed config in Gateway `DownstreamServices` (optional, for Ocelot route merging)
3. Call `POST /api/service-registry/register` from the service at startup
4. Gateway immediately discovers the service for health checks

The Gateway `ServiceRegistry` merges static config and dynamic registrations. Health checks, Swagger fetching, and service listing use the combined view.

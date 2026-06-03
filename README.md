# DotnetNiger

Open-source community platform built with .NET microservices, Ocelot API Gateway, and OpenIddict authentication.

A modern, modular platform for the .NET Niger community — featuring posts, events, resources, member profiles, newsletters, multi-tenant administration, and a developer portal.

## Architecture Overview

```
Client (Browser / Mobile / API)
        │
        ▼
┌─────────────────────┐    Port 5000
│   API Gateway       │   (Ocelot)
│   DotnetNiger       │   Routing, Auth, Rate-Limiting,
│   .Gateway          │   QoS, Swagger Aggregation
└────────┬────────────┘
         │
     ┌───┴───┐
     ▼       ▼
┌────────┐ ┌──────────┐
│Identity│ │ Community │
│ :5075  │ │ :5050     │
│(:8081) │ │(:8082)    │
└────────┘ └──────────┘

Also:
  Identity.Web — Developer Portal :5100
  TestIdentity — OIDC test app     :5200
```

All external traffic goes through the **Gateway** (port `5000`). Downstream services are not exposed directly in production.

## Services

| Service         | Dev Port | Container Port | Description                                                                                  |
| --------------- | -------- | -------------- | -------------------------------------------------------------------------------------------- |
| **Gateway**     | `5000`   | `5000`         | Ocelot API Gateway — routing, JWT auth, rate limiting, health checks, Swagger merge          |
| **Identity**    | `5075`   | `8081`         | Authentication, multi-tenant users, roles, permissions, OAuth2/OIDC (OpenIddict)             |
| **Community**   | `5050`   | `8082`         | Community content — posts, events, resources, comments, newsletters, member profiles, search |
| **Identity.Web** | `5100` | —              | Developer portal UI — login, dashboard, admin, profile, security                             |
| **TestIdentity** | `5200` | —              | OIDC connection test app — validates the Identity Server OAuth flow                          |

## Quick Start

```bash
git clone https://github.com/akaletekoffilevis/DotnetNiger.git
cd DotnetNiger
dotnet restore
```

Start services in order (4 terminals):

```bash
# Terminal 1 — Identity
cd DotnetNiger.Identity && dotnet run

# Terminal 2 — Community
cd DotnetNiger.Community && dotnet run

# Terminal 3 — Gateway
cd DotnetNiger.Gateway && dotnet run

# Terminal 4 — Identity.Web (optional, developer portal)
cd DotnetNiger.Identity.Web && dotnet run
```

## Key URLs

| URL                                                       | Description                                       |
| --------------------------------------------------------- | ------------------------------------------------- |
| `http://localhost:5000/swagger`                           | Aggregated Swagger UI (all services)              |
| `http://localhost:5000/health`                            | Gateway health                                    |
| `http://localhost:5000/health/downstream`                 | All downstream services health (static + dynamic) |
| `http://localhost:5000/health/services`                   | Registered services configuration                 |
| `http://localhost:5075/swagger`                           | Identity Swagger (direct)                         |
| `http://localhost:5050/swagger`                           | Community Swagger (direct)                        |
| `http://localhost:5100`                                   | Identity.Web developer portal                     |
| `http://localhost:5200`                                   | TestIdentity OIDC test app                        |

## Tech Stack

| Component    | Technology                                               |
| ------------ | -------------------------------------------------------- |
| Runtime      | .NET 9.0                                                 |
| Gateway      | Ocelot 24.x, MMLib.SwaggerForOcelot, Polly, CacheManager |
| Identity     | ASP.NET Core Identity, OpenIddict 5.x, EF Core + SQLite  |
| Community    | ASP.NET Core, EF Core + SQLite, JWT Bearer               |
| Identity.Web | ASP.NET Core Razor Pages, OIDC auth                      |
| Auth         | JWT (asymmetric RSA) + OpenIddict OIDC — JWS tokens     |
| Logging      | Serilog (Console + File)                                 |
| Email        | MailKit (SMTP)                                           |
| Social Login | Google, Microsoft, GitHub OAuth                          |
| Container    | Docker, docker-compose                                   |

## Documentation

- [docs/INDEX.md](docs/INDEX.md) — Documentation index
- [docs/SETUP.md](docs/SETUP.md) — Local setup, prerequisites, service startup
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — Microservices architecture, dependency rules
- [docs/API.md](docs/API.md) — API endpoints, routing, conventions
- [DotnetNiger.Gateway/README.md](DotnetNiger.Gateway/README.md)
- [DotnetNiger.Identity/README.md](DotnetNiger.Identity/README.md)
- [DotnetNiger.Identity/INTEGRATION_GUIDE.md](DotnetNiger.Identity/INTEGRATION_GUIDE.md)
- [DotnetNiger.Identity.Web/README.md](DotnetNiger.Identity.Web/README.md)
- [DotnetNiger.Community/README.md](DotnetNiger.Community/README.md)
- [DotnetNiger.Community/INTEGRATION_GUIDE.md](DotnetNiger.Community/INTEGRATION_GUIDE.md)
- [DotnetNiger.TestIdentity/README.md](DotnetNiger.TestIdentity/README.md)

## License

MIT License — see [LICENSE.md](LICENSE.md)

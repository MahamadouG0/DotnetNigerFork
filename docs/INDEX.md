# DotnetNiger Documentation Index

Central documentation for the DotnetNiger monorepo.

## Quick Links

| Document                           | Description                                  |
| ---------------------------------- | -------------------------------------------- |
| [SETUP.md](SETUP.md)               | Local setup, prerequisites, service startup  |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Microservices architecture, dependency rules |
| [API.md](API.md)                   | API endpoints, routing, conventions          |

## Service Documentation

| Service          | README                                                                  | Integration Guide                                                                        |
| ---------------- | ----------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| **Gateway**      | [README](../DotnetNiger.Gateway/README.md)                              | —                                                                                        |
| **Identity**     | [README](../DotnetNiger.Identity/README.md)                             | [INTEGRATION_GUIDE](../DotnetNiger.Identity/INTEGRATION_GUIDE.md)                        |
| **Community**    | [README](../DotnetNiger.Community/README.md)                            | [INTEGRATION_GUIDE](../DotnetNiger.Community/INTEGRATION_GUIDE.md)                       |
| **Identity.Web** | [README](../DotnetNiger.Identity.Web/README.md)                         | —                                                                                        |
| **TestIdentity** | [README](../DotnetNiger.TestIdentity/README.md)                         | —                                                                                        |

## Scope

This documentation covers:

- **DotnetNiger.Gateway** — API Gateway (Ocelot)
- **DotnetNiger.Identity** — Auth service (OpenIddict)
- **DotnetNiger.Community** — Community content service
- **DotnetNiger.Identity.Web** — Developer Portal (Razor Pages)
- **DotnetNiger.TestIdentity** — OIDC test client
- **DotnetNiger.Tests** — Unit and integration tests

## Source of Truth

In case of discrepancies, prioritize:

1. Configuration files (`appsettings.*.json`)
2. CI/CD workflows (`.github/workflows/`)
3. Architecture tests (`DotnetNiger.Tests/DotnetNiger.Architecture.Tests/`)

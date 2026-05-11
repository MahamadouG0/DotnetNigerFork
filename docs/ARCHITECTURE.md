# Architecture

## Vue d'ensemble

DotnetNiger est organise en microservices .NET 8:

- Gateway (Ocelot): point d'entree unique, aggregation Swagger, routage, rate-limiting, health endpoints.
- Identity: authentification/authorization, utilisateurs, roles, permissions, API keys, account deletion.
- Community: contenus communautaires (posts, comments, events, resources, projects, tags, categories, newsletters).

## Regles de dependances

### Regle principale

La couche Application ne doit pas referencer `Infrastructure.Repositories`.

### Enforcement

Cette regle est enforcee par des tests dans:

- [DotnetNiger.Architecture.Tests/ApplicationLayerDependencyGuardsTests.cs](../DotnetNiger.Architecture.Tests/ApplicationLayerDependencyGuardsTests.cs)

Les workflows CI executent ces gardes dans un job dedie `architecture-guards`.

## Pattern utilise

Pour Community et Identity:

- Les services Application consomment des abstractions de persistance (Application/Abstractions/Persistence).
- Les repositories Infrastructure implementent ces abstractions.
- L'injection de dependances mappe abstraction -> implementation dans les extensions de services API.

## Communication inter-services

- Community appelle Identity via un client HTTP typé.
- Gateway route les appels vers Identity et Community via Ocelot.

## Sante et observabilite

- Endpoints `/health`, `/health/downstream`, `/health/ready` sur Gateway.
- Endpoint latency metrics expose via `/metrics/latency` sur Gateway et services.
- Logs structurees via Serilog.

## Fichiers cle

- [DotnetNiger.Gateway/Program.cs](../DotnetNiger.Gateway/Program.cs)
- [DotnetNiger.Gateway/ocelot.json](../DotnetNiger.Gateway/ocelot.json)
- [DotnetNiger.Identity/Program.cs](../DotnetNiger.Identity/Program.cs)
- [DotnetNiger.Community/Program.cs](../DotnetNiger.Community/Program.cs)
- [DotnetNiger.Community/Api/Extensions/ServiceExtensions.cs](../DotnetNiger.Community/Api/Extensions/ServiceExtensions.cs)
- [DotnetNiger.Identity/Api/Extensions/ServiceExtensions.cs](../DotnetNiger.Identity/Api/Extensions/ServiceExtensions.cs)

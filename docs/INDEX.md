# Documentation DotnetNiger

Ce dossier centralise la documentation technique actuelle du monorepo.

## Fichiers

- [SETUP.md](SETUP.md): installation locale, prerequis, execution des services.
- [ARCHITECTURE.md](ARCHITECTURE.md): architecture microservices et regles de dependances.
- [API.md](API.md): routes principales, URLs et conventions API.
- [HEALTH_REPORT.md](HEALTH_REPORT.md): etat de sante technique et controles CI.

## Portee

Cette documentation couvre:

- DotnetNiger.Gateway
- DotnetNiger.Identity
- DotnetNiger.Community
- DotnetNiger.Architecture.Tests

## Source de verite

La source de verite reste le code. En cas d'ecart, prioriser:

1. Les fichiers de configuration runtime ([DotnetNiger.Gateway/appsettings.Development.json](../DotnetNiger.Gateway/appsettings.Development.json), [DotnetNiger.Identity/appsettings.Development.json](../DotnetNiger.Identity/appsettings.Development.json), [DotnetNiger.Community/appsettings.Development.json](../DotnetNiger.Community/appsettings.Development.json))
2. Les workflows CI ([.github/workflows/ci.yml](../.github/workflows/ci.yml), [.github/workflows/tests.yml](../.github/workflows/tests.yml))
3. Les tests d'architecture ([DotnetNiger.Architecture.Tests/ApplicationLayerDependencyGuardsTests.cs](../DotnetNiger.Architecture.Tests/ApplicationLayerDependencyGuardsTests.cs))

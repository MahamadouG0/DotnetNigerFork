# Health Report

Date de reference: 2026-04-11

## Resume executif

Etat global: Stable avec garde-fous d'architecture actifs.

- Build solution: OK
- Tests d'architecture: OK (2/2)
- CI principale: configuree pour restore/build/test sur la solution
- Job CI dedie architecture: actif

## Controles en place

### CI

- [ .github/workflows/ci.yml ](../.github/workflows/ci.yml)
  - restore solution
  - build solution
  - test solution

- [ .github/workflows/tests.yml ](../.github/workflows/tests.yml)
  - job `architecture-guards` dedie
  - tests + couverture
  - build quality avec warnings as errors

### Architecture

- [DotnetNiger.Architecture.Tests/ApplicationLayerDependencyGuardsTests.cs](../DotnetNiger.Architecture.Tests/ApplicationLayerDependencyGuardsTests.cs)
  - Interdit `DotnetNiger.Community.Infrastructure.Repositories` dans Community/Application
  - Interdit `DotnetNiger.Identity.Infrastructure.Repositories` dans Identity/Application

## Endpoints de sante runtime

- Gateway: `/health`, `/health/downstream`, `/health/ready`
- Identity: `/api/v1/diagnostics/health`
- Community: `/api/v1/test/health`

## Risques residuels

- Les artefacts `bin/obj/logs` peuvent reapparaitre apres build/tests (normal).
- La robustesse des docs repose sur leur maintenance manuelle lors des changements de routes/config.

## Actions recommandees

1. Ajouter un controle de coherence docs (checklist PR).
2. Continuer a executer les tests d'architecture avant merge.
3. Ajouter des tests contractuels API gateway -> downstream pour routes critiques.

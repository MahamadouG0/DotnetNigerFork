# Changelog

Tous les changements notables de DotnetNiger sont documentes dans ce fichier.

Le format suit Keep a Changelog et le versioning suit Semantic Versioning.

## [Unreleased]

### Changed

- Maintenance continue des services Identity, Community et Gateway.
- Mises a jour ponctuelles de documentation et de configuration selon les besoins d'integration.

## [1.4.0] - 2026-03-14

### Added

- Community: industrialisation de la couche API et ajout de DTOs requests dedies.
- Identity: alignement des conventions de reponses et d'erreurs avec Community.

### Changed

- Projet: finalisation de la documentation transverse et mise a jour de la configuration.

### Removed

- Nettoyage d'artefacts obsoletes dans le depot.

## [1.3.0] - 2026-03-11

### Added

- Community: migration fonctionnelle Team -> Member.
- Community: versioning API v1 avec routes api/v{version}/... et configuration JWT.
- Documentation: guides d'integration Blazor WASM et mise a jour du setup/health/index.

### Changed

- Community: reorganisation des services et interfaces pour clarifier les responsabilites.

## [1.2.0] - 2026-03-07

### Added

- Gateway: routage Ocelot natif avec JWT, rate limiting, QoS et cache.
- Identity: enrichissement des endpoints admin et consolidation des routes de gestion utilisateurs.
- Infrastructure: configuration base SQLite partagee entre Identity et Community.

### Changed

- Documentation: re-ecriture de l'architecture et des guides pour refléter le gateway Ocelot natif.
- Scripts: consolidation de l'automatisation service/base dans run.sh.

### Removed

- Suppression de l'implementation gateway YARP depreciee.
- Nettoyage de controllers/dependances depreciees cote Identity.

## [1.1.0] - 2026-02-20

### Added

- Community: domaine complet (entites, enums, interfaces, DTOs) et controllers API.
- Identity: securite renforcee (HMAC API key, hash refresh tokens, middleware JWT, seeds roles/permissions/admin).
- Gateway: integration explicite des services et middlewares dans le pipeline.

### Changed

- Identity: refactor des services avec repository pattern et meilleure separation des responsabilites.
- Community: DI, repositories EF Core SQLite et seeding de donnees de test.

### Fixed

- Tests et configuration JWT: corrections de signatures et de cles minimales.
- Git/config: ajustements .gitignore et formatage pipeline.

## [1.0.0] - 2026-01-29

### Added

- Initialisation du projet et de l'architecture microservices.
- Premiers workflows CI/CD et outillage de formatage.
- Documentation de base du projet et du setup.

### Changed

- Iterations rapides sur la structure, README et workflows des les premiers jours.

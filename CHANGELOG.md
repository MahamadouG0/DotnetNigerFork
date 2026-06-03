# Changelog

Tous les changements notables de DotnetNiger sont documentes dans ce fichier.

Le format suit Keep a Changelog et le versioning suit Semantic Versioning.

## [Unreleased]

### Added

- TestIdentity: `Pages/Account/Login.cshtml.cs` — explicit OIDC challenge with PKCE, COCONUT + S256
- TestIdentity: `Pages/Shared/_Layout.cshtml` — Bootstrap 5 UI with nav, footer, icons
- TestIdentity: `Pages/_ViewStart.cshtml` — layout assignment
- Identity/DbSeeder: `test-client` confidential client (password + client_credentials grants), `OAuthTestUser`, `TenantClient` registration, `scp:api` permission
- Identity/AuthController: `HandleClientCredentialsGrantAsync` — custom grant handler resolving clients via `TenantClients`
- Community: new entity types seeded via `DbSeeder` (Projects, Partners, Newsletter, Members, EventTags, ResourceTags)
- Community: `AddJwtBearer` with `ValidateAudience = false`, local JWKS validation (no callback to Identity per request)
- Identity.Web/Program.cs: `ConfigKeyPath` fallback handling

### Changed

- Identity: OpenIddict tokens now emit **JWS** (signed JSON Web Tokens) instead of JWE (encrypted) — `DisableAccessTokenEncryption()` enabled
- Identity/ApplicationSetup: `MigrateAsync()` → `EnsureCreatedAsync()` (no migration files in project)
- Identity/ServiceExtensions: suppressed `PendingModelChangesWarning` in DbContext config
- Community/AppDbContext: `IsRequired(false)` on navigation properties to suppress EF Core warnings
- Gateway: config key corrected in `ServiceCollectionExtensions` and `ApplicationBuilderExtensions`
- Gateway/ocelot.identity.routes.json: duplicated `IdentityAuthRoute` → `IdentitySuperAdminRoute`

### Fixed

- Community CRUD operations returning 401: resolved by switching Identity to JWS tokens (Community's `AddJwtBearer` expects standard JWT format)
- NuGet cache for Identity restored: 19 OpenIddict 5.8.0 packages, FluentValidation 11.11.0, MailKit 4.16.0, Polly 8.3.0 copied to local cache enabling Identity rebuild from source
- TestIdentity/Logout: corrected to use OpenID Connect end-session endpoint
- Gateway: `app.Map("/api/v1/diagnostics/health", ...)` → `app.Use(...)` for correct middleware registration
- Gateway/ocelot.json: `BaseUrl` corrected to `http://localhost:5000`

### Security

- Identity: JWT tokens are now standard JWS (3-part: header.payload.signature) signed with RS256 — compatible with any standard JWT library
- Identity: encryption key retained in configuration (OpenIddict 5.x requirement) but token encryption disabled
- `test-client` confidential client credentials flow available for machine-to-machine integration (`IServiceRegistry` / `ServiceRegistry`) pour la decouverte dynamique de services avec `ConcurrentDictionary` thread-safe.
- Gateway: `POST /api/service-registry/register` endpoint middleware pour l'enregistrement dynamique des services upstream (avec auth optionnelle via `X-Registration-Key`).
- Gateway: `Services/ServiceRegistry.cs` et `Services/ServiceRegistrationEndpoint.cs` — nouveaux fichiers pour le registre de services et le endpoint d'enregistrement.
- Identity: Auto-enregistrement aupres du Gateway au demarrage via `TryRegisterWithGatewayAsync()` (config `Gateway:RegistrationUrl` + `Gateway:RegistrationKey`).
- Community: Auto-enregistrement aupres du Gateway au demarrage via `TryRegisterWithGatewayAsync()` (config `Gateway:RegistrationUrl` + `Gateway:RegistrationKey`).
- Identity: `[InternalApiKeyAuth]` attribute — protege les endpoints `_internal` avec l'en-tete `X-Internal-Key`.
- Identity: `SlugAlreadyExistsException` / `EmailAlreadyExistsException` → HTTP 409 Conflict (au lieu de 400 BadRequest).
- Tests: `InternalAuthTests` (3) — validation de l'auth interne.
- Tests: `ProfileServiceTests` (7) — couverture du service Community ProfileService.

### Changed

- Gateway: `MapGatewayHealthEndpoints()` utilise desormais `IServiceRegistry.GetCombinedConfig()` au lieu d'une liste statique — les services dynamiques apparaissent dans `/health/downstream`, `/health/ready`, `/health/services`.
- Gateway: Swagger merge middleware utilise le registre de services (`IServiceRegistry`) pour decouvrir les endpoints Swagger des services upstream.
- Gateway: `Program.cs` cree un `ServiceRegistry` initialise depuis la config statique `DownstreamServices` et l'enregistre en singleton DI.
- Gateway: `DownstreamServiceConfig` reste utilise pour les routes Ocelot (construites au demarrage), le registre dynamique se superpose pour les health checks.
- docs/ARCHITECTURE.md: Mise a jour complete de la section "Dynamic Service Discovery" avec le nouveau modele a deux niveaux (statique + dynamique).
- docs/SETUP.md: Ajout des instructions de configuration du Gateway Registration, variables `Gateway:RegistrationUrl`/`Gateway:RegistrationKey`.
- DotnetNiger.Gateway/README.md: Ajout de la section Service Discovery, mise a jour du project structure, endpoints, et pipeline middleware.
- `UserService` : toutes les methodes acceptent desormais `tenantId` et valident `user.TenantId` (protection cross-tenant).
- `ExternalServiceService` : les services sont crees avec le statut `Active` (etait `Pending`).
- Cache key : `ExternalServiceHealthService` utilise `ext:{slug}` (etait `ext:{Guid}`).

### Fixed

- Community: Correction de la variable `combined` non declaree dans `SearchService.SearchAsync()` — ajout de `IQueryable<SearchResultResponse>? combined = null;`.
- Identity/AuthController.UserInfo : valide que le `tenant_id` claim correspond au tenant de l'utilisateur.
- Gateway/ocelot.identity.routes.json : cle dupliquee `IdentityAuthRoute` renommee en `IdentitySuperAdminRoute`.
- Gateway/ocelot.json `BaseUrl` : corrige de `localhost:5050` vers `http://localhost:5000`.
- Gateway/CORS : fallback gracieux quand `Cors:AllowedOrigins` n'est pas configure.
- Gateway/ExternalServiceHealthService : envoie `X-Internal-Key` lors des appels aux endpoints `_internal`.
- Gateway/OcelotConfigurationBuilder.BindUrls : `BaseUrl` dur en `http://localhost:5000` (etait le premier `DevUrl` de la liste).

### Security

- Gateway: Le endpoint `/api/service-registry/register` peut etre protege par une cle API via `Gateway:RegistrationKey` (envoyee dans le header `X-Registration-Key`).
- ApiKeyAuthenticationHandler : verifie desormais `TenantContext.TenantId` avant la recherche de cle API (defense-in-depth).
- `_internal` endpoints : proteges par `[InternalApiKeyAuth]` — retourne 401 sans en-tete `X-Internal-Key` valide.
- `UpdateHealthStatusAsync` : protege via `[InternalApiKeyAuth]` sur le controller.

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

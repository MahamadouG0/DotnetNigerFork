# Architecture DotnetNiger Platform

## Les Deux Piliers

### Pilier 1 : Identity Provider (Auth-as-a-Service)

**But :** Être un fournisseur OIDC/OAuth2 où d'autres sites redirigent leurs utilisateurs pour l'authentification, et où des APIs externes valident leurs tokens.

| Capacité                                                                                | Statut   |
| --------------------------------------------------------------------------------------- | -------- |
| `POST /connect/token` (password, client_credentials, authorization_code, refresh_token) | ✅       |
| `GET /connect/userinfo`                                                                 | ✅       |
| `GET /connect/authorize`                                                                | ✅       |
| `POST /connect/logout`                                                                  | ✅       |
| `POST /api/v1/auth/register-tenant` (inscription multi-tenant)                          | ✅       |
| Forgot / Reset password                                                                 | ✅       |
| External providers (Google, GitHub, Microsoft)                                          | ✅       |
| Clés API (TenantApiKey) — CRUD                                                          | ✅       |
| Clients OAuth2 (TenantClient) — CRUD                                                    | ✅       |
| Roles & Permissions engine                                                              | ✅       |
| Rate limiting                                                                           | ✅       |
| **Dashboard intégrateur (docs, stats)**                                                 | ✅       |
| **Page login personnalisable (branding)**                                               | 🔮 Futur |
| **Email confirmation**                                                                  | 🔮 Futur |

### Pilier 2 : Developer Portal (Services DotnetNiger)

**But :** Les développeurs créent un compte sur Identity pour utiliser les services de la plateforme DotnetNiger.

| Service                                                                                   | Statut |
| ----------------------------------------------------------------------------------------- | ------ |
| **Gateway dynamique** — Exposer ses APIs externes via `/ext/{slug}/**`                    | ✅     |
| **ExternalService** — Enregistrement d'API externe (register, list, edit, delete, health) | ✅     |
| **ApiKeyAuthenticationHandler** — Auth X-API-Key                                          | ✅     |
| **Dashboard développeur** — Voir ses services, clés, statuts                              | ✅     |
| **Documentation multi-langage** — cURL, Java, Node, Python, Go, Rust, PHP, Ruby, C#       | ✅     |
| **Admin UI** — Tenants CRUD, Users CRUD, Roles & Permissions, Clients OAuth2              | ✅     |
| **System stats** — Dashboard admin enrichi                                                | ✅     |

## Déploiement multi-serveur

Chaque service est un projet .NET indépendant, déployable séparément :

| Service                  | Port dev | Port Docker | Dockerfile | Base de données                          |
| ------------------------ | -------- | ----------- | ---------- | ---------------------------------------- |
| Identity (OpenIddict)    | 5075     | 8081        | Oui        | SQLite dev, SQL Server / PostgreSQL prod |
| Identity.Web             | 5100     | —           | Non        | Aucune (appelle l'API Identity)          |
| Gateway (Ocelot)         | 5000     | 5000        | Oui        | Aucune (cache Redis optionnel)           |
| Community (Blog, Events) | 5050     | 8082        | Oui        | SQLite dev, SQL Server / PostgreSQL prod |

## Règles de dépendance (enforced by tests)

- Gateway → Identity (OIDC validation + ExternalService lookup)
- Gateway → Community (route proxy)
- Identity.Web → Identity (API calls via OIDC + Bearer token)
- Community → Identity (TODO: OIDC token validation)
- **Community ne doit PAS référencer Identity directement** (sauf validation OpenIddict)
- **Gateway ne doit PAS référencer Community directement**

## Stack technique

| Composant       | Technologie                                  |
| --------------- | -------------------------------------------- |
| Runtime         | .NET 9.0                                     |
| OAuth/OIDC      | OpenIddict 5.8.0                             |
| API Gateway     | Ocelot                                       |
| ORM             | EF Core 9.0                                  |
| Frontend        | Razor Pages + Bootstrap 5                    |
| Base de données | SQLite (dev), SQL Server / PostgreSQL (prod) |
| Multi-DB        | DatabaseProvider pattern                     |

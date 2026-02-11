# Architecture DotnetNiger

**Vue d'ensemble complète du projet microservices DotnetNiger (.NET 8+) avec API Gateway YARP.**

---

## Table des matières

1. [Vue d'ensemble](#vue-densemble)
2. [Structure générale](#structure-générale)
3. [Services détaillés](#services-détaillés)
4. [Clean Architecture](#clean-architecture)
5. [Structure des fichiers](#structure-des-fichiers)
6. [Communication](#communication)
7. [Données et stockage](#données-et-stockage)
8. [Configuration](#configuration)

---

## Vue d'ensemble

```
┌─────────┐
│ Client  │
└────┬────┘
     │
     v
┌──────────────────────────────────────┐
│     Gateway (DotnetNiger.Gateway)    │
│         YARP + Swagger Aggregator    │
└──────┬──────────────────┬────────────┘
       │                  │
       v                  v
┌────────────────────┐  ┌──────────────────────┐
│ Identity Service   │  │ Community Service    │
│ Authentication     │  │ Social Features      │
│ Users & Tokens     │  │ Posts, Comments      │
└─────────┬──────────┘  └──────────┬───────────┘
          │                        │
          v                        v
 ┌────────────────┐       ┌────────────────┐
 │  SQL Server    │       │  SQL Server    │
 │  (Identity DB) │       │ (Community DB) │
 └────────────────┘       └────────────────┘
          |                        |
          └────────────┬───────────┘
                       |
                       v
                  ┌─────────┐
                  │ Redis   │
                  │ Cache   │
                  └─────────┘
```

---

## Structure générale

```
DotnetNiger/
│
├── docs/                                 # Documentation
│   ├── ARCHITECTURE.md (ce fichier)
│   ├── API.md
│   ├── INDEX.md
│   └── SETUP.md
│
├── DotnetNiger.Gateway/                 # Service Gateway
│   ├── Program.cs                       # Point d'entrée, configuration DI
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── appsettings.Production.json
│   ├── Dockerfile
│   ├── DotnetNiger.Gateway.csproj
│   │
│   ├── Api/
│   │   ├── Controllers/
│   │   │   └── SwaggerAggregatorController.cs
│   │   ├── Extensions/
│   │   ├── Filters/
│   │   └── Middleware/
│   │
│   ├── Application/
│   │   ├── DTOs/
│   │   ├── Exceptions/
│   │   └── Services/
│   │
│   ├── Infrastructure/
│   │   ├── Caching/
│   │   ├── CircuitBreaker/              # Resilience patterns
│   │   ├── Config/
│   │   ├── HttpClients/
│   │   └── Monitoring/
│   │
│   └── Configuration/
│       └── yarp-routes.json             # YARP routing config
│
├── DotnetNiger.Identity/                # Service Identity
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Dockerfile
│   ├── DotnetNiger.Identity.http       # REST Client requests
│   │
│   ├── Api/
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs       # Login, Register
│   │   │   ├── UsersController.cs      # User management
│   │   │   ├── TokensController.cs     # Token operations
│   │   │   ├── RolesController.cs      # Role management
│   │   │   ├── AdminController.cs      # Admin operations
│   │   │   ├── SocialLinksController.cs
│   │   │   └── TestControllers.cs
│   │   ├── Extensions/
│   │   ├── Filters/
│   │   └── Middleware/
│   │
│   ├── Application/
│   │   ├── DTOs/
│   │   ├── Exceptions/
│   │   ├── Mappers/
│   │   ├── Services/
│   │   │   ├── AuthService.cs
│   │   │   ├── UserService.cs
│   │   │   ├── TokenService.cs
│   │   │   ├── RoleService.cs
│   │   │   ├── PasswordService.cs
│   │   │   ├── EmailService.cs
│   │   │   ├── LoginHistoryService.cs
│   │   │   ├── SocialLinkService.cs
│   │   │   └── Interfaces/
│   │   └── Validators/
│   │
│   ├── Domain/
│   │   ├── Entities/
│   │   │   ├── ApplicationUser.cs       # User principal
│   │   │   ├── Role.cs
│   │   │   ├── Permission.cs
│   │   │   ├── RolePermission.cs
│   │   │   ├── RefreshToken.cs
│   │   │   ├── ApiKey.cs
│   │   │   ├── LoginHistory.cs
│   │   │   └── SocialLink.cs
│   │   ├── Enums/
│   │   └── Interfaces/
│   │
│   └── Infrastructure/
│       ├── Data/
│       │   ├── DotnetNigerIdentityDbContext.cs
│       │   ├── DotnetNigerIdentityDbFactory.cs
│       │   └── Seeds/
│       ├── Repositories/
│       ├── Caching/
│       ├── External/
│       └── Security/
│
├── DotnetNiger.Community/              # Service Community
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Dockerfile
│   ├── DotnetNiger.Community.http
│   │
│   ├── Api/
│   │   ├── Controllers/
│   │   │   ├── PostsController.cs      # Posts CRUD
│   │   │   ├── CommentsController.cs   # Comments
│   │   │   ├── CategoriesController.cs
│   │   │   ├── TagsController.cs
│   │   │   ├── EventsController.cs     # Events management
│   │   │   ├── PartnersController.cs
│   │   │   ├── ProjectsController.cs
│   │   │   ├── ResourcesController.cs
│   │   │   ├── SearchController.cs     # Search functionality
│   │   │   ├── StatsController.cs      # Statistics
│   │   │   ├── TeamController.cs
│   │   │   └── TestControllers.cs
│   │   ├── Extensions/
│   │   ├── Filters/
│   │   └── Middleware/
│   │
│   ├── Application/
│   │   ├── DTOs/
│   │   ├── Exceptions/
│   │   ├── Mappers/
│   │   ├── Services/
│   │   │   ├── PostService.cs
│   │   │   ├── CommentService.cs
│   │   │   ├── CategoryService.cs
│   │   │   ├── TagService.cs
│   │   │   ├── EventService.cs
│   │   │   ├── PartnerService.cs
│   │   │   ├── ProjectService.cs
│   │   │   ├── ResourceService.cs
│   │   │   ├── SearchService.cs
│   │   │   ├── StatisticsService.cs
│   │   │   ├── TeamService.cs
│   │   │   ├── IdentityApiClient.cs    # Call Identity service
│   │   │   └── Interfaces/
│   │   └── Validators/
│   │
│   ├── Domain/
│   │   ├── Entities/
│   │   │   ├── Post.cs                 # Social posts
│   │   │   ├── Comment.cs
│   │   │   ├── Category.cs
│   │   │   ├── Tag.cs
│   │   │   ├── Event.cs                # Events
│   │   │   ├── EventMedia.cs
│   │   │   ├── EventRegistration.cs
│   │   │   ├── Partner.cs
│   │   │   ├── Project.cs
│   │   │   ├── ProjectContributor.cs
│   │   │   ├── Resource.cs
│   │   │   ├── ResourceCategory.cs
│   │   │   ├── PostCategory.cs
│   │   │   ├── PostTag.cs
│   │   │   ├── TeamMember.cs
│   │   │   └── TeamMemberSkill.cs
│   │   ├── Enums/
│   │   └── Interfaces/
│   │
│   └── Infrastructure/
│       ├── Data/
│       │   ├── CommunityDbContext.cs
│       │   ├── CommunityDbContextFactory.cs
│       │   └── Seeds/
│       ├── Repositories/
│       ├── Caching/
│       └── External/
│
├── docker-compose.yml                   # Orchestration services
│
├── run.ps1                              # PowerShell launch script
├── run.sh                               # Bash launch script
│
├── package.json                         # Node/npm dependencies
├── CHANGELOG.md
├── LICENSE.md
├── README.md
├── SECURITY.md
│
└── DotnetNiger.slnx                     # Solution file
```

---

## Services détaillés

### 1. **Gateway (DotnetNiger.Gateway)**

**Rôle:** Point d'entrée centraliser, routage, agrégation API.

| Aspect              | Détail                                                             |
| ------------------- | ------------------------------------------------------------------ |
| **Port**            | Configurable (appsettings)                                         |
| **Technologie**     | YARP (Yet Another Reverse Proxy)                                   |
| **Responsabilités** | Routage → Identity, Community; Agrégation Swagger; Filtres globaux |
| **Controllers**     | SwaggerAggregatorController                                        |
| **Infrastructure**  | CircuitBreaker, Caching, HttpClients, Monitoring                   |

**Routes YARP** (Configuration/yarp-routes.json):

- `/identity/*` → DotnetNiger.Identity
- `/community/*` → DotnetNiger.Community

---

### 2. **Identity Service (DotnetNiger.Identity)**

**Rôle:** Authentification, gestion des utilisateurs, émission de tokens.

| Aspect    | Détail                                    |
| --------- | ----------------------------------------- |
| **DB**    | SQL Server (DotnetNigerIdentityDbContext) |
| **Cache** | Redis (tokens, sessions)                  |
| **Port**  | Configurable                              |

**Entities principales:**

- `ApplicationUser` - Utilisateur principal
- `Role` - Rôles utilisateur
- `Permission`, `RolePermission` - Permissions granulaires
- `RefreshToken` - Tokens de rafraîchissement
- `ApiKey` - Clés API pour appels tiers
- `LoginHistory` - Historique des connexions
- `SocialLink` - Liens sociaux utilisateur

**Controllers:**

| Endpoint            | Responsabilité          |
| ------------------- | ----------------------- |
| `/api/auth`         | Login, Register, Logout |
| `/api/users`        | CRUD user, profile      |
| `/api/tokens`       | Refresh token, validate |
| `/api/roles`        | Gestion des rôles       |
| `/api/admin`        | Opérations admin        |
| `/api/social-links` | Réseaux sociaux         |

**Services clés:**

- `AuthService` - Logique authentification
- `UserService` - Gestion utilisateurs
- `TokenService` - JWT/tokens
- `RoleService` - Gestion rôles
- `PasswordService` - Hachage, validation
- `EmailService` - Emails (verification, reset)
- `LoginHistoryService` - Suivi connexions
- `SocialLinkService` - Liens sociaux

---

### 3. **Community Service (DotnetNiger.Community)**

**Rôle:** Fonctionnalités sociales, posts, événements, projects.

| Aspect    | Détail                               |
| --------- | ------------------------------------ |
| **DB**    | SQL Server (CommunityDbContext)      |
| **Cache** | Redis (posts populaires, recherches) |
| **Port**  | Configurable                         |

**Entities principales:**

- **Posts & Engagement:** Post, Comment, PostCategory, PostTag
- **Events:** Event, EventMedia, EventRegistration
- **Community:** Partner, TeamMember, TeamMemberSkill
- **Resources:** Project, ProjectContributor, Resource, ResourceCategory
- **Tagging:** Tag, Category

**Controllers:**

| Endpoint          | Responsabilité                |
| ----------------- | ----------------------------- |
| `/api/posts`      | CRUD posts, likes, engagement |
| `/api/comments`   | CRUD comments                 |
| `/api/events`     | Gestion événements            |
| `/api/projects`   | Gestion projets               |
| `/api/partners`   | Partenaires                   |
| `/api/resources`  | Ressources, docs              |
| `/api/categories` | Catégories                    |
| `/api/tags`       | Tags, hashtags                |
| `/api/teams`      | Équipes, membres              |
| `/api/search`     | Recherche full-text           |
| `/api/stats`      | Statistiques, analytics       |

**Services clés:**

- `PostService` - Posts et engagement
- `CommentService` - Commentaires
- `EventService` - Événements
- `ProjectService` - Projets communautaires
- `TeamService` - Gestion équipes
- `SearchService` - Recherche
- `StatisticsService` - Analytics
- `IdentityApiClient` - Appel Identity service
- `CategoryService`, `TagService` - Taxonomies

---

## Clean Architecture

Chaque service suit la segregation en 4 couches:

### **1. Api Layer** (`Api/`)

```
Controllers/          # HTTP endpoints
├── *.Controller.cs   # [ApiController], routes
Extensions/           # IServiceCollection extensions
├── ServiceCollection.cs
Filters/              # [ActionFilter], exceptions
├── ExceptionFilter.cs
├── ValidationFilter.cs
Middleware/           # Request/Response pipeline
├── ErrorHandlingMiddleware.cs
├── LoggingMiddleware.cs
```

**Responsabilités:**

- Exposes les endpoints HTTP
- Valide les requêtes entrantes
- Serialise/désérialise JSON
- Gère les codes HTTP

---

### **2. Application Layer** (`Application/`)

```
DTOs/                 # Data Transfer Objects
├── LoginRequest.cs
├── CreatePostDto.cs
Services/             # Business logic, orchestration
├── UserService.cs
├── PostService.cs
Validators/           # FluentValidation rules
├── CreatePostValidator.cs
Mappers/              # Entity ↔ DTO mappings
├── UserProfile.cs    # AutoMapper profiles
Exceptions/           # Custom exceptions
├── ValidationException.cs
├── NotFoundException.cs
Interfaces/           # Service contracts
├── IUserService.cs
```

**Responsabilités:**

- Use cases métier
- DTOs pour requêtes/réponses
- Validation métier
- Orchestration services domain

---

### **3. Domain Layer** (`Domain/`)

```
Entities/             # Core business entities
├── User.cs
├── Post.cs
├── Role.cs
Enums/                # Énumérations métier
├── RoleEnum.cs
├── PostStatus.cs
Interfaces/           # Repository contracts
├── IUserRepository.cs
├── IRepository<T>.cs
```

**Responsabilités:**

- Entités métier pures (sans dépendances)
- Règles métier implicites
- Interfaces repositories (contracts)
- Enums, value objects

---

### **4. Infrastructure Layer** (`Infrastructure/`)

```
Data/                 # EF Core DbContext
├── *DbContext.cs
├── *DbFactory.cs
├── Seeds/            # Initial data
├── Migrations/       # EF Core migrations
Repositories/         # Repository implementations
├── UserRepository.cs
├── BaseRepository.cs
Caching/              # Redis, in-memory cache
├── CacheService.cs
External/             # 3rd party APIs
├── EmailProvider.cs
├── SmsProvider.cs
Security/             # Encryption, hashing (Identity only)
├── PasswordHasher.cs
```

**Responsabilités:**

- Accès données (EF Core)
- Implémentation repositories
- Cache
- Appels services externes
- Hachage, chiffrement

---

## Structure des fichiers

### Racine projet

```
DotnetNiger/
├── DotnetNiger.slnx                # Solution unique
├── package.json                    # Scripts npm, version management
├── docker-compose.yml              # Services (SQL, Redis)
├── run.ps1 / run.sh               # Scripts lancement
├── CHANGELOG.md                   # Historique versions
├── LICENSE.md                     # Licence du projet
├── README.md                      # Guide global
├── SECURITY.md                    # Sécurité, bonnes pratiques
└── docs/                          # Documentation
    ├── ARCHITECTURE.md (ce fichier)
    ├── API.md
    ├── SETUP.md
    └── INDEX.md
```

### Par service

Chaque service (`Identity`, `Community`, `Gateway`) contient:

```
DotnetNiger.[Service]/
├── Program.cs                    # ASP.NET Core host builder
├── appsettings.json              # Config par défaut
├── appsettings.Development.json  # Config développement
├── appsettings.Production.json   # Config production (Gateway, Community)
├── Dockerfile                    # Image Docker
├── DotnetNiger.[Service].csproj  # Projet .NET
├── [Service].http                # REST Client (Identity, Community)
│
├── Api/
├── Application/
├── Domain/ (Identity, Community)
├── Infrastructure/
└── Properties/
    └── launchSettings.json       # Debug ports
```

---

## Communication

### Flux requête HTTP

```
1. Client
   ↓
2. Gateway (port 5000 par défaut)
   ├─ Valide, applique filtres
   ├─ Route vers service
   ↓
3. Service cible (Identity ou Community)
   ├─ Reçoit requête
   ├─ Controllers → Application → Domain → Infrastructure
   ├─ Accède DB/Cache
   ↓
4. Réponse remonte
   ↓
5. Client reçoit
```

### Appels inter-services

**Community → Identity:**

- `IdentityApiClient.cs` en Infrastructure/Services
- Appelle l'API Identity pour valider tokens, récupérer user info
- Utilise HttpClient typé (DI container)

```csharp
// Exemple: Community récupère infos user via Identity
var userInfo = await _identityClient.GetUserAsync(userId);
```

---

## Données et stockage

### Bases de données

| Service       | Base                   | Context                        | Migrations |
| ------------- | ---------------------- | ------------------------------ | ---------- |
| **Identity**  | `DotnetNigerIdentity`  | `DotnetNigerIdentityDbContext` | EF Core    |
| **Community** | `DotnetNigerCommunity` | `CommunityDbContext`           | EF Core    |

**Isolation:**

- Chaque service sa BD
- Pas de requêtes croisées directes
- Communication via API REST

### Cache (Redis)

Utilisé pour:

- Sessions utilisateur
- Tokens (pour rapidité)
- Posts populaires, recherches
- Données temporaires

Configuration: `appsettings.json` (`Redis:ConnectionString`)

### DbFactory

Chaque service a un `*DbFactory.cs`:

```csharp
DotnetNigerIdentityDbFactory.cs
CommunityDbContextFactory.cs
```

Pour migrations, seeding, et tests.

---

## Configuration

### appsettings.json

Structure commune à tous les services:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Jwt": {
    "SecretKey": "...",
    "Issuer": "...",
    "Audience": "...",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

### YARP Configuration (Gateway)

**Configuration/yarp-routes.json:**

```json
{
  "ReverseProxy": {
    "Routes": {
      "identityRoute": {
        "ClusterId": "identityCluster",
        "Match": { "Path": "/identity/{**catch-all}" }
      },
      "communityRoute": {
        "ClusterId": "communityCluster",
        "Match": { "Path": "/community/{**catch-all}" }
      }
    },
    "Clusters": {
      "identityCluster": {
        "Destinations": {
          "identity": { "Address": "https://localhost:5001" }
        }
      },
      ...
    }
  }
}
```

### Environnements

- **Development** (`appsettings.Development.json`)
  - Logs verbeux
  - CORS ouvert
  - SQL Server local
  - Swagger activé

- **Production** (`appsettings.Production.json`)
  - Logs minimaux
  - CORS restreint
  - SQL Server cloud/distant
  - Swagger désactivé (optionnel)

---

## Points clefs et patterns

✅ **Microservices independants**

- Bases de données séparées
- Déploiement indépendant
- Scaling par service

✅ **Clean Architecture**

- Séparation des responsabilités claire
- Testabilité élevée
- Maintenance facilitée

✅ **Gateway centralisée**

- Point d'entrée unique pour clients
- Routage transparent
- Concerns transverses (logging, monitoring)

✅ **Communication asynchrone capable**

- Appels HTTP synchrones actuellement
- Architecture prête pour pub/sub (message queue)

✅ **Repository Pattern**

- Abstraction sur data access
- Facilite tests et migrations DB

✅ **Dependency Injection**

- Inversion of Control dans `Program.cs`
- Extensions pour chaque couche

✅ **Extensibilité**

- Ajouter un nouveau service = copier structure, enregistrer route YARP
- Partage patterns et conventions

---

## Portes par défaut (development)

```
Gateway:     https://localhost:5000
Identity:    https://localhost:5001
Community:   https://localhost:5002
SQL Server:  tcp://localhost:1433
Redis:       localhost:6379
```

> Configurable dans `launchSettings.json` et docker-compose.

---

## Prochaines étapes pour orientation

1. **Lancer le projet:** Voir [SETUP.md](SETUP.md)
2. **APIs disponibles:** Voir [API.md](API.md)
3. **Index documentation:** Voir [INDEX.md](INDEX.md)

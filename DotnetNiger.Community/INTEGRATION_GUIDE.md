# Guide d'Intégration — DotnetNiger.Community

API publique de la communauté DotnetNiger — Posts, Events, Resources, Comments, Search, Profile, Admin.

## Prérequis

- .NET 9.0+
- [DotnetNiger.Identity](https://github.com/akaletekoffilevis/DotnetNiger) en cours d'exécution sur `http://localhost:5075`
- JWT Bearer token obtenu via Identity (`/connect/token`)

## Architecture

```
DotnetNiger.Community/
├── Api/
│   ├── Controllers/       → 7 controllers (versionnés /api/v1/)
│   ├── Middleware/         → Error handling
│   └── ServiceExtensions.cs → DI registration
├── Application/
│   ├── DTOs/              → Request/Response DTOs
│   └── Services/          → Business logic + IdentityApiClient
├── Domain/
│   └── Entities/          → 13 entités (Post, Event, Comment, etc.)
├── Infrastructure/
│   └── AppDbContext.cs    → EF Core DbContext (SQLite)
└── Program.cs
```

## Démarrage

```bash
cd DotnetNiger.Community
dotnet run
```

Service disponible sur `http://localhost:5000`.
Swagger UI : `http://localhost:5000/` (développement uniquement).

## Endpoints disponibles

### Posts

| Méthode | Endpoint | Auth | Description |
|---------|----------|------|-------------|
| GET | `/api/v1/Posts` | Non | Liste paginée des posts |
| GET | `/api/v1/Posts/{id}` | Non | Détail d'un post |
| POST | `/api/v1/Posts` | Oui | Créer un post |
| PUT | `/api/v1/Posts/{id}` | Oui | Modifier un post |

### Events

| Méthode | Endpoint | Auth | Description |
|---------|----------|------|-------------|
| GET | `/api/v1/Events` | Non | Liste paginée des événements |
| GET | `/api/v1/Events/upcoming` | Non | Prochains événements |
| GET | `/api/v1/Events/{id}` | Non | Détail d'un événement |
| POST | `/api/v1/Events` | Oui | Créer un événement |
| PUT | `/api/v1/Events/{id}` | Oui | Modifier un événement |
| POST | `/api/v1/Events/{eventId}/registrations` | Oui | S'inscrire à un événement |
| GET | `/api/v1/Events/registrations` | Oui | Mes inscriptions |
| GET | `/api/v1/Events/{eventId}/registrations` | Oui | Inscriptions à un événement (admin) |

### Resources

| Méthode | Endpoint | Auth | Description |
|---------|----------|------|-------------|
| GET | `/api/v1/Resources` | Non | Liste paginée des ressources |
| GET | `/api/v1/Resources/{id}` | Non | Détail d'une ressource |
| POST | `/api/v1/Resources` | Oui | Créer une ressource |
| PUT | `/api/v1/Resources/{id}` | Oui | Modifier une ressource |
| POST | `/api/v1/Resources/{id}/views` | Non | Incrémenter les vues |

### Comments

| Méthode | Endpoint | Auth | Description |
|---------|----------|------|-------------|
| GET | `/api/v1/Comments/post/{postId}` | Non | Commentaires d'un post |
| GET | `/api/v1/Comments/event/{eventId}` | Non | Commentaires d'un événement |
| GET | `/api/v1/Comments/{id}` | Non | Détail d'un commentaire |
| POST | `/api/v1/Comments` | Oui | Créer un commentaire |
| PUT | `/api/v1/Comments/{id}` | Oui | Modifier un commentaire |
| DELETE | `/api/v1/Comments/{id}` | Oui | Supprimer un commentaire |

### Profile

| Méthode | Endpoint | Auth | Description |
|---------|----------|------|-------------|
| GET | `/api/v1/me` | Oui | Mon profil (membre) |
| PUT | `/api/v1/me` | Oui | Mettre à jour mon profil |
| GET | `/api/v1/social-links` | Oui | Mes liens sociaux |
| POST | `/api/v1/social-links` | Oui | Ajouter un lien social |
| DELETE | `/api/v1/social-links/{id}` | Oui | Supprimer un lien social |

### Search

| Méthode | Endpoint | Auth | Description |
|---------|----------|------|-------------|
| GET | `/api/v1/Search?q=...` | Non | Recherche globale (posts, events, resources) |

### Admin

| Méthode | Endpoint | Auth | Description |
|---------|----------|------|-------------|
| GET | `/api/v1/admin/dashboard` | Admin | Statistiques du dashboard |
| GET | `/api/v1/admin/users` | Admin | Liste des utilisateurs |
| GET | `/api/v1/admin/users/{id}` | Admin | Détail d'un utilisateur |
| PATCH | `/api/v1/admin/users/{id}/status` | Admin | Activer/désactiver un utilisateur |
| GET | `/api/v1/admin/roles` | Admin | Liste des rôles |
| POST | `/api/v1/admin/roles` | Admin | Créer un rôle |
| GET | `/api/v1/admin/permissions` | Admin | Liste des permissions |
| POST | `/api/v1/admin/permissions` | Admin | Créer une permission |
| POST | `/api/v1/admin/roles/{roleId}/permissions` | Admin | Assigner des permissions à un rôle |
| POST | `/api/v1/admin/users/{userId}/roles` | Admin | Assigner un rôle à un utilisateur |
| PATCH | `/api/v1/admin/events/{id}/publish` | Admin | Publier un événement |
| PATCH | `/api/v1/admin/events/{id}/unpublish` | Admin | Dépublier un événement |

## Format des réponses

Tous les endpoints retournent une réponse uniforme :

```json
{
  "success": true,
  "data": { ... }
}
```

Pour les listes paginées :

```json
{
  "success": true,
  "data": {
    "items": [],
    "totalCount": 0,
    "page": 1,
    "pageSize": 10,
    "totalPages": 0,
    "hasNextPage": false,
    "hasPreviousPage": false
  }
}
```

## Authentification

Les endpoints `Auth` utilisent le JWT Bearer. Obtenez un token depuis Identity :

```bash
TOKEN=$(curl -s -X POST http://localhost:5075/connect/token \
  -d "grant_type=password&username=admin@dotnetniger.com&password=Admin@123456&scope=openid+profile+email+roles+offline_access" \
  | jq -r '.access_token')

curl -s http://localhost:5000/api/v1/admin/dashboard \
  -H "Authorization: Bearer $TOKEN"
```

## Configuration

Dans `appsettings.json` :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=DotnetNigerCommunity.db"
  },
  "Jwt": {
    "Authority": "http://localhost:5075",
    "MetadataAddress": "http://localhost:5075/.well-known/openid-configuration"
  },
  "Identity": {
    "BaseUrl": "http://localhost:5075"
  }
}
```

## Compte admin de test

- **Email :** `admin@dotnetniger.com`
- **Mot de passe :** `Admin@123456`

(Utilisateur créé par Identity via son DbSeeder.)

# DotnetNiger.Community

API publique de la communauté DotnetNiger — Posts, Events, Resources, Comments, Search, Profile, Admin.

## Technologies

- .NET 9.0
- ASP.NET Core (Controllers, JWT Bearer auth)
- Entity Framework Core + SQLite
- Swashbuckle (Swagger / OpenAPI)
- Identity API client (proxie vers DotnetNiger.Identity)

## Architecture

La structure suit celle de DotnetNiger.Identity :

```
Api/Controllers/       → Endpoints REST (versionnés /api/v1/)
Api/Middleware/        → Error handling
Api/ServiceExtensions  → Enregistrement DI
Application/DTOs/      → DTOs requête/réponse
Application/Services/  → Logique métier
Domain/Entities/       → Entités EF Core
Infrastructure/        → DbContext, seeder
```

## Démarrage

```bash
cd DotnetNiger.Community
dotnet run
```

Service disponible sur `http://localhost:5000`.
Swagger UI : `http://localhost:5000/` (développement).

## Dépendances

- [DotnetNiger.Identity](https://github.com/akaletekoffilevis/DotnetNiger) — authentification JWT, gestion des utilisateurs/rôles/permissions

## Documentation intégration

Voir [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md) pour le guide complet d'intégration client (endpoints, auth, exemples cURL).

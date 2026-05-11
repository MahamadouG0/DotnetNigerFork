# DotnetNiger.Gateway

API Gateway Ocelot pour la plateforme DotnetNiger.

Derniere mise a jour: 2026-03-14

## Stack

- .NET 8
- Ocelot
- MMLib.SwaggerForOcelot
- Ocelot.Cache.CacheManager
- JWT Bearer auth
- Serilog

## Ports

- Gateway: <http://localhost:5000>
- Identity downstream: <http://localhost:5075>
- Community downstream: <http://localhost:5269>

## Demarrage

```bash
dotnet restore
dotnet run
```

## Endpoints

- GET /swagger
- GET /health
- GET /info

## Routes cles

- Identity: /api/auth/_, /api/users/_, /api/admin/_, /api/diagnostics/_
- Community: /api/posts/_, /api/events/_, /api/projects/_, /api/resources/_
- Admin Community: /api/admin/community/\*

## Rate limiting et priorites

- Rate limiting explicite sur les routes non limitees
- /health et /info sans limitation
- Priorites admin:
  - IdentityAdminRoute priorite 1
  - CommunityAdminRoute priorite 2

# DotnetNiger.Identity

Service Identity pour l'authentification et la gestion des utilisateurs.

## Demarrage rapide

```bash
dotnet restore
cd DotnetNiger.Identity
dotnet ef database update
./run.ps1    # Windows
./run.sh     # Linux/Mac
```

Swagger: http://localhost:5075/swagger

## Endpoints utiles

- POST /api/v1/auth/login
- GET /api/v1/users/me
- POST /api/v1/users/me/avatar
- GET /api/v1/users/me/avatar
- DELETE /api/v1/users/me/avatar

## Upload avatar

Le provider est configurable via `FileUpload:Provider`:

- `Local`: stocke sur disque et expose via `/uploads`
- `Azure`: stocke sur Azure Blob Storage

Exemple (Local):

```json
"FileUpload": {
  "Provider": "Local",
  "RootPath": "uploads",
  "PublicBasePath": "/uploads",
  "MaxAvatarBytes": 2000000,
  "AllowedAvatarContentTypes": ["image/jpeg", "image/png", "image/webp"],
  "AllowedAvatarExtensions": [".jpg", ".jpeg", ".png", ".webp"],
  "CleanupEnabled": false,
  "CleanupIntervalMinutes": 1440,
  "CleanupOrphanDays": 7,
  "Azure": {
    "ConnectionString": "",
    "Container": "dotnetniger-uploads",
    "PublicBaseUrl": ""
  }
}
```

## Tests

```bash
dotnet test DotnetNiger.Identity.Tests
dotnet test DotnetNiger.Identity.IntegrationTests
```

## Secrets

Utilise user-secrets ou des variables d'environnement:

```bash
dotnet user-secrets set "Jwt:Key" "<jwt-secret>" --project DotnetNiger.Identity
dotnet user-secrets set "Email:Smtp:Password" "<smtp-password>" --project DotnetNiger.Identity
```

```bash
Jwt__Key=<jwt-secret>
Email__Smtp__Password=<smtp-password>
```

## Fichiers utiles

- [DotnetNiger.Identity.http](DotnetNiger.Identity.http) - requetes REST
- [docs/API.md](../docs/API.md) - documentation API
- [docs/SETUP.md](../docs/SETUP.md) - setup global

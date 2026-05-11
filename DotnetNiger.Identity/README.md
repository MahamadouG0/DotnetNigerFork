# DotnetNiger.Identity

Service d'authentification et d'autorisation multi-tenant basé sur **ASP.NET Core Identity** + **OpenIddict** (OAuth2/OIDC).

## Technologies

- .NET 9.0
- ASP.NET Core Identity (users, rôles, email)
- OpenIddict (OAuth2/OIDC — password flow, refresh token)
- SQLite (EF Core)
- Swagger / OpenAPI
- Serilog
- FluentValidation
- MailKit (SMTP)
- Google, Microsoft, GitHub OAuth

## Démarrage

```bash
cd DotnetNiger.Identity
dotnet run
```

Service disponible sur `http://localhost:5075`.

## Endpoints principaux

| Méthode | Endpoint | Description |
|---------|----------|-------------|
| POST | `/connect/token` | Obtenir un JWT (password/refresh token) |
| POST | `/api/v1/auth/register` | Créer un compte |
| POST | `/api/v1/auth/login` | Login JSON (validation) |
| POST | `/api/v1/auth/confirm-email` | Confirmer l'email |
| GET | `/api/v1/auth/userinfo` | Infos utilisateur connecté |
| GET | `/api/v1/diagnostics/health` | Health check |
| GET | `/.well-known/openid-configuration` | Métadonnées OIDC |
| GET | `/.well-known/jwks` | Clés publiques RSA |

## Configuration

Utiliser `user-secrets` pour les clés sensibles :

```bash
dotnet user-secrets set "Smtp:Password" "votre-mot-de-passe"
dotnet user-secrets set "Authentication:Google:ClientId" "..."
dotnet user-secrets set "Authentication:Google:ClientSecret" "..."
```

## Documentation intégration

Voir [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md) pour le guide complet d'intégration client (JWT, social login, multi-tenant, endpoints).

# DotnetNiger.Identity

Authentication and authorization microservice built on **ASP.NET Core Identity** + **OpenIddict** (OAuth2 / OpenID Connect). Provides multi-tenant user management, role-based access control, social login, and token-based authentication.

## Tech Stack

- .NET 9.0
- ASP.NET Core Identity (users, roles, email confirmation)
- OpenIddict 5.x (OAuth2 / OIDC — password, refresh token, client credentials grants)
- Entity Framework Core 9.x + SQLite
- Swashbuckle / Swagger (OpenAPI)
- Serilog (structured logging)
- FluentValidation (request validation)
- MailKit (SMTP email)
- Social OAuth: Google, Microsoft, GitHub

## Project Structure

```
DotnetNiger.Identity/
├── Program.cs
├── Api/
│   ├── Controllers/          → Auth, Profile, Admin, Roles, Permissions, Users, Tenants, Diagnostics
│   └── ServiceExtensions.cs   → DI registration
├── Application/
│   ├── DTOs/                  → Request/Response models
│   └── Services/              → Business logic (AuthService, etc.)
├── Domain/
│   └── Entities/              → ApplicationUser, etc.
└── Infrastructure/
    └── Data/                  → DbContext, seeding
```

## Quick Start

```bash
cd DotnetNiger.Identity
dotnet run
```

Service available at `http://localhost:5075`. Swagger: `http://localhost:5075/swagger`.

On first run, the database is auto-created and seeded with:

- **Super Admin**: `admin@dotnetniger.com` / `Admin@123456`
- Default roles: `Admin`, `User`
- Default permissions

## Key Endpoints

| Method | Endpoint                            | Auth   | Description                                      |
| ------ | ----------------------------------- | ------ | ------------------------------------------------ |
| POST   | `/connect/token`                    | —      | OAuth2 token endpoint (password / refresh_token) |
| POST   | `/api/v1/auth/register`             | —      | Create account                                   |
| POST   | `/api/v1/auth/confirm-email`        | —      | Verify email (JSON body)                         |
| GET    | `/api/v1/auth/confirm-email`        | —      | Verify email (query string)                      |
| POST   | `/api/v1/auth/resend-code`          | —      | Resend confirmation code                         |
| POST   | `/api/v1/auth/login`                | —      | JSON login (validates credentials)               |
| POST   | `/api/v1/auth/logout`               | Bearer | Logout                                           |
| GET    | `/api/v1/auth/userinfo`             | Bearer | Current user info                                |
| GET    | `/api/v1/auth/external-login`       | —      | Redirect to OAuth provider                       |
| GET    | `/api/v1/auth/external-callback`    | —      | OAuth callback                                   |
| GET    | `/api/v1/profile`                   | Bearer | Get own profile                                  |
| PUT    | `/api/v1/profile`                   | Bearer | Update profile                                   |
| DELETE | `/api/v1/profile`                   | Bearer | Delete own account                               |
| GET    | `/api/v1/diagnostics/health`        | —      | Health check                                     |
| GET    | `/.well-known/openid-configuration` | —      | OIDC metadata                                    |
| GET    | `/.well-known/jwks`                 | —      | Public RSA keys (JWKS)                           |

### Admin Endpoints

| Method | Endpoint                         | Auth  | Description                |
| ------ | -------------------------------- | ----- | -------------------------- |
| GET    | `/api/v1/admin/stats`            | Admin | Platform statistics        |
| CRUD   | `/api/v1/admin/tenants`          | Admin | Tenant management          |
| CRUD   | `/api/v1/{tenantId}/users`       | Admin | User management per tenant |
| CRUD   | `/api/v1/{tenantId}/roles`       | Admin | Role management per tenant |
| CRUD   | `/api/v1/{tenantId}/permissions` | Admin | Permission management      |

## Multi-Tenancy

Tenant isolation is enforced via:

- The `tenant_id` claim in the JWT
- The `X-Tenant-Id` header for public endpoints
- All queries are automatically filtered by tenant context

## Social Login

Supports Google, Microsoft, and GitHub OAuth. Configure via:

```json
{
  "Authentication": {
    "Google": { "ClientId": "...", "ClientSecret": "..." },
    "Microsoft": { "ClientId": "...", "ClientSecret": "..." },
    "GitHub": { "ClientId": "...", "ClientSecret": "..." }
  }
}
```

Providers activate automatically when their `ClientId` is non-empty.

## Gateway Self-Registration

On startup, Identity automatically registers with the Gateway via `POST /api/service-registry/register`:

```json
{
  "Gateway": {
    "RegistrationUrl": "http://localhost:5000/api/service-registry/register",
    "RegistrationKey": "__SET_VIA_ENV_OR_USER_SECRETS__"
  }
}
```

If the Gateway is unavailable at startup, the service logs a warning and continues normally. Registration is non-fatal.

## Configuration

Use `dotnet user-secrets` for sensitive values:

```bash
dotnet user-secrets set "Smtp:Password" "your-password"
dotnet user-secrets set "Authentication:Google:ClientId" "your-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-secret"
dotnet user-secrets set "Gateway:RegistrationKey" "your-gateway-key"
```

## Integration

See [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md) for complete client integration documentation including:

- JWT token acquisition and refresh
- Email confirmation flow
- Social login implementation
- Multi-tenant API usage
- Complete cURL examples

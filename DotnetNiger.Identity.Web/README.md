# DotnetNiger.Identity.Web

Developer portal UI for the DotnetNiger platform — an ASP.NET Core Razor Pages application that authenticates via OpenID Connect against the Identity Server.

Provides the developer-facing dashboard, admin panels, API key management, profile editing, account security page, and documentation.

## Tech Stack

- .NET 9.0
- ASP.NET Core Razor Pages
- OpenID Connect authentication (code flow)
- Cookie authentication (session management)

## Quick Start

```bash
cd DotnetNiger.Identity.Web
dotnet run
```

Available at `http://localhost:5100`. Requires the Identity Server (`http://localhost:5075`) to be running.

Test credentials: `admin@dotnetniger.com` / `Admin@123456`

## Configuration

### appsettings.json

```json
{
  "Identity": {
    "BaseUrl": "http://localhost:5075",
    "ClientId": "web-ui",
    "ClientSecret": ""
  },
  "DeveloperPortal": {
    "GatewayBaseUrl": "http://localhost:5000"
  }
}
```

### User Secrets

```bash
cd DotnetNiger.Identity.Web
dotnet user-secrets set "Identity:ClientSecret" "your-client-secret"
```

| Key | Required | Default | Description |
|-----|----------|---------|-------------|
| `Identity:BaseUrl` | Yes | — | Identity Server base URL |
| `Identity:ClientId` | Yes | `web-ui` | OIDC client ID (registered in Identity) |
| `Identity:ClientSecret` | No | — | OIDC client secret |
| `DeveloperPortal:GatewayBaseUrl` | No | — | Gateway URL for API calls |

## Pages

| URL | Description |
|-----|-------------|
| `/` | Home page |
| `/Developer/Index` | Developer portal hub |
| `/Developer/Dashboard` | Dashboard with stats |
| `/Developer/ApiKeys` | API key CRUD |
| `/Developer/Services` | External services management |
| `/Developer/Docs` | Integration docs |
| `/Developer/Profile` | Edit profile + change password |
| `/Developer/Securite` | Account security overview |
| `/Developer/Admin/Index` | Admin dashboard |
| `/Developer/Admin/Tenants` | Tenant CRUD |
| `/Developer/Admin/Tenants/{id}/Users` | User management |
| `/Developer/Admin/Tenants/{id}/Roles` | Role & permission management |
| `/Developer/Admin/Tenants/{id}/Clients` | OAuth2 client list |
| `/Developer/Admin/Tenants/{id}/ApiKeys` | API key management |
| `/Account/Login` | Login |
| `/Account/Logout` | Logout |
| `/Account/AccessDenied` | Access denied |
| `/Status` | Health status page |
| `/Securite` | Security policy |
| `/Confidentialite` | Privacy policy |
| `/ConditionsUtilisation` | Terms of service |
| `/Support` | Support page |

## OIDC Flow

The portal uses the OpenID Connect authorization code flow:

1. Unauthenticated users are redirected to Identity Server's `/connect/authorize`
2. After login, Identity Server redirects back with an authorization code
3. The code is exchanged for tokens at `/connect/token`
4. User info is fetched from the `/userinfo` endpoint
5. Sessions are managed via cookies (8-hour expiry, sliding expiration)

## Project Structure

```
DotnetNiger.Identity.Web/
├── Program.cs
├── Pages/
│   ├── Index.cshtml              # Home page
│   ├── Error.cshtml              # Error page (French)
│   ├── Status.cshtml             # Health status
│   ├── Account/                  # Login, Logout, AccessDenied
│   ├── Developer/
│   │   ├── Index.cshtml          # Portal hub
│   │   ├── Dashboard.cshtml      # Stats dashboard
│   │   ├── ApiKeys.cshtml        # API key management
│   │   ├── Services.cshtml       # External services
│   │   ├── Docs.cshtml           # Documentation
│   │   ├── Profile.cshtml        # Profile + change password
│   │   ├── Securite.cshtml       # Security overview
│   │   └── Admin/                # Admin panels
│   └── Shared/
└── wwwroot/
```

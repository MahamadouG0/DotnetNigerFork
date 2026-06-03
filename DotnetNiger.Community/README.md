# DotnetNiger.Community

Community content service for the DotnetNiger platform — posts, events, resources, comments, newsletters, member profiles, search, and administration.

## Tech Stack

- .NET 9.0
- ASP.NET Core (Controllers, JWT Bearer authentication)
- Entity Framework Core 9.x + SQLite
- Swashbuckle / Swagger (OpenAPI)
- Serilog (structured logging)

## Project Structure

```
DotnetNiger.Community/
├── Program.cs
├── Api/
│   ├── Controllers/          → 7 controllers (Posts, Events, Comments, Resources, Search, Profile, Admin)
│   ├── Middleware/            → ErrorHandlingMiddleware
│   └── ServiceExtensions.cs   → DI registration
├── Application/
│   ├── DTOs/                  → Request/Response models
│   └── Services/              → Business logic
├── Domain/
│   └── Entities/              → EF Core entities (13 entities)
└── Infrastructure/
    ├── AppDbContext.cs         → EF Core DbContext
    └── Seed/                   → Database seeder
```

## Quick Start

```bash
cd DotnetNiger.Community
dotnet run
```

Service available at `http://localhost:5050`. Swagger: `http://localhost:5050/swagger`.

Requires [DotnetNiger.Identity](https://github.com/akaletekoffilevis/DotnetNiger) for JWT authentication (must be running on `http://localhost:5075`).

## API Endpoints

All routes are prefixed with `/api/v1/`.

### Posts

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/Posts` | — | List with pagination, filtering |
| GET | `/Posts/{id}` | — | Get by ID |
| POST | `/Posts` | Bearer | Create post |
| PUT | `/Posts/{id}` | Bearer | Update post |
| DELETE | `/Posts/{id}` | Bearer | Delete post |

### Events

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/Events` | — | List with filters |
| GET | `/Events/upcoming` | — | Upcoming events |
| GET | `/Events/{id}` | — | Get by ID |
| POST | `/Events` | Bearer | Create event |
| PUT | `/Events/{id}` | Bearer | Update event |
| DELETE | `/Events/{id}` | Bearer | Delete event |
| POST | `/Events/registrations` | Bearer | Register for event |
| DELETE | `/Events/{eventId}/registrations` | Bearer | Cancel registration |
| GET | `/Events/{eventId}/registrations` | Bearer | List registrations |

### Resources

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/Resources` | — | List with filters |
| GET | `/Resources/{id}` | — | Get by ID |
| POST | `/Resources` | Bearer | Create resource |
| PUT | `/Resources/{id}` | Bearer | Update resource |
| DELETE | `/Resources/{id}` | Bearer | Delete resource |
| POST | `/Resources/{id}/views` | — | Increment view count |

### Comments

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/Comments/post/{postId}` | — | Comments for a post |
| GET | `/Comments/event/{eventId}` | — | Comments for an event |
| GET | `/Comments/{id}` | — | Get by ID |
| POST | `/Comments` | Bearer | Create comment |
| PUT | `/Comments/{id}` | Bearer | Update comment |
| DELETE | `/Comments/{id}` | Bearer | Delete comment |

### Profile

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/v1/me` | Bearer | Get member profile |
| PUT | `/api/v1/me` | Bearer | Update profile |
| GET | `/api/v1/social-links` | Bearer | List social links |
| POST | `/api/v1/social-links` | Bearer | Add social link |
| DELETE | `/api/v1/social-links/{id}` | Bearer | Delete social link |

### Search

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/Search?q=...` | — | Full-text search across posts, events, resources |

### Newsletters

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/newsletters/subscribe` | — | Subscribe |
| POST | `/newsletters/quick-subscribe` | — | Quick subscribe (email only) |
| POST | `/newsletters/verify` | — | Verify subscription |
| POST | `/newsletters/unsubscribe` | — | Unsubscribe |
| POST | `/newsletters/register` | — | Register with details |
| GET | `/newsletters/subscriptions` | Bearer | List subscriptions (admin) |
| GET/PATCH/DELETE | `/newsletters/subscriptions/{id}` | Bearer | Manage subscription (admin) |

### Admin

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/admin/dashboard` | Admin | Dashboard statistics |
| GET | `/admin/users` | Admin | List users |
| GET | `/admin/users/{id}` | Admin | Get user |
| PATCH | `/admin/users/{id}/status` | Admin | Activate/deactivate user |
| GET | `/admin/roles` | Admin | List roles |
| POST | `/admin/roles` | Admin | Create role |
| GET | `/admin/permissions` | Admin | List permissions |
| POST | `/admin/permissions` | Admin | Create permission |
| POST | `/admin/roles/{roleId}/permissions` | Admin | Assign permissions to role |
| POST | `/admin/users/{userId}/roles` | Admin | Assign role to user |
| PATCH | `/admin/events/{id}/publish` | Admin | Publish event |
| PATCH | `/admin/events/{id}/unpublish` | Admin | Unpublish event |

### Other

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/projects/{everything}` | — | Projects CRUD |
| GET | `/resources/{everything}` | — | Resources CRUD |
| GET | `/categories/{everything}` | — | Categories CRUD |
| GET | `/tags/{everything}` | — | Tags CRUD |
| GET | `/partners/{everything}` | — | Partners CRUD |
| GET | `/members/{everything}` | — | Members CRUD |
| GET | `/stats/{everything}` | — | Statistics |
| GET | `/test/health` | — | Health check |

## Response Format

All endpoints return a uniform response:

```json
{
  "success": true,
  "data": { ... }
}
```

Paginated responses:

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

## Authentication

Protected endpoints require a JWT Bearer token obtained from the Identity service:

```bash
TOKEN=$(curl -s -X POST http://localhost:5075/connect/token \
  -d "grant_type=password&username=admin@dotnetniger.com&password=Admin@123456&scope=openid+profile+email+roles+offline_access" \
  | jq -r '.access_token')

curl -s http://localhost:5269/api/v1/admin/dashboard \
  -H "Authorization: Bearer $TOKEN"
```

## Dependencies

- **DotnetNiger.Identity** — JWT validation (`Authority` / `MetadataAddress`), user/role provisioning via HTTP client

## Gateway Self-Registration

On startup, Community automatically registers with the Gateway via `POST /api/service-registry/register`:

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
  },
  "Gateway": {
    "RegistrationUrl": "http://localhost:5000/api/service-registry/register",
    "RegistrationKey": "__SET_VIA_ENV_OR_USER_SECRETS__"
  }
}
```

## Integration

See [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md) for complete API usage, authentication examples, and cURL samples.

# API Reference

## Base URLs

| Environment | Gateway | Identity | Community | Identity.Web | TestIdentity |
|-------------|---------|----------|-----------|--------------|--------------|
| Development | `http://localhost:5000` | `http://localhost:5075` | `http://localhost:5050` | `http://localhost:5100` | `http://localhost:5200` |
| Docker | `http://localhost:5000` | `http://localhost:8081` | `http://localhost:8082` | — | — |

In production, **only the Gateway** port (`5000`) should be exposed. Downstream services are internal.

## Swagger UIs

| URL | Description |
|-----|-------------|
| `http://localhost:5000/swagger` | Aggregated (all services) |
| `http://localhost:5075/swagger` | Identity only (direct) |
| `http://localhost:5050/swagger` | Community only (direct) |

## Gateway Endpoints

### Health, Metrics & Service Registry

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Gateway liveness |
| GET | `/health/ready` | Readiness (all downstreams must respond) |
| GET | `/health/downstream` | Detailed downstream health (static + dynamic services) |
| GET | `/health/services` | Registered services config (static + dynamic) |
| POST | `/api/service-registry/register` | Dynamic service registration |
| GET | `/metrics/latency` | Endpoint latency (P50/P95/P99) |

### External Service Proxy

| Method | Endpoint | Description |
|--------|----------|-------------|
| `{any}` | `/ext/{slug}/{everything}` | Proxy to registered external service by slug |

External services register via the Identity API and are cached by the Gateway using `ext:{slug}` cache keys (60s TTL).

---

## Identity API — Direct (`http://localhost:5075/api/v1`)

### Authentication

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/auth/register-tenant` | — | Self-service tenant registration (creates tenant + admin + OAuth client + API key) |
| POST | `/auth/forgot-password` | — | Request password reset email |
| POST | `/auth/reset-password` | — | Reset password with token |
| POST | `/auth/register` | — | Register a new user (auto-assigned to tenant) |

### Profile

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/profile` | Bearer | Get current user profile |
| PUT | `/profile` | Bearer | Update profile (firstName, lastName, avatarUrl) |
| DELETE | `/profile` | Bearer | Delete current user account |

### External Services

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/external-services/register` | ApiKey + Bearer | Register a new external service |
| GET | `/external-services` | ApiKey + Bearer | List services for current tenant |
| GET | `/external-services/{id}` | ApiKey + Bearer | Get service by ID |
| PATCH | `/external-services/{id}` | ApiKey + Bearer | Update service (baseUrl, description, healthEndpoint) |
| DELETE | `/external-services/{id}` | ApiKey + Bearer | Delete a service |
| GET | `/external-services/by-slug/{slug}` | Anonymous | Resolve slug → baseUrl (used by Gateway) |
| GET | `/external-services/_internal/active` | `X-Internal-Key` | List all active services (Gateway health poll) |
| POST | `/external-services/_internal/{id}/health-result` | `X-Internal-Key` | Report health check result from Gateway |

### Admin — Tenants

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/admin/tenants` | Admin | Create a new tenant |
| GET | `/admin/tenants` | Admin | List all tenants |
| GET | `/admin/tenants/{id}` | Admin | Get tenant by ID |
| GET | `/admin/tenants/by-slug/{slug}` | Admin | Get tenant by slug |
| PUT | `/admin/tenants/{id}` | Admin | Update tenant (name, description, isActive) |
| DELETE | `/admin/tenants/{id}` | Admin | Delete tenant and all associated data |

### Admin — Users (per tenant)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/{tenantId}/users` | Admin | Create a new user in the tenant |
| GET | `/{tenantId}/users` | Admin | List all users in the tenant |
| GET | `/{tenantId}/users/{id}` | Admin | Get user by ID |
| PUT | `/{tenantId}/users/{id}` | Admin | Update user (firstName, lastName, isActive) |
| DELETE | `/{tenantId}/users/{id}` | Admin | Delete user |
| POST | `/{tenantId}/users/{id}/change-password` | Admin | Change user password |

### Admin — Roles (per tenant)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/{tenantId}/roles` | Admin | Create a new role |
| GET | `/{tenantId}/roles` | Admin | List all roles in the tenant |
| PUT | `/{tenantId}/roles/{id}` | Admin | Update role (description) |
| DELETE | `/{tenantId}/roles/{id}` | Admin | Delete role |
| POST | `/{tenantId}/roles/{roleId}/users/{userId}` | Admin | Assign user to role |
| DELETE | `/{tenantId}/roles/{roleId}/users/{userId}` | Admin | Remove user from role |
| GET | `/{tenantId}/roles/user/{userId}` | Admin | Get roles for a user |

### Admin — Permissions (per tenant)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/{tenantId}/permissions` | Admin | Create a new permission |
| GET | `/{tenantId}/permissions` | Admin | List all permissions |
| GET | `/{tenantId}/permissions/grouped` | Admin | List permissions grouped by category |
| DELETE | `/{tenantId}/permissions/{id}` | Admin | Delete permission |
| POST | `/{tenantId}/permissions/assign` | Admin | Assign permissions to a role |

### Admin — API Keys (per tenant)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/admin/tenants/{tenantId}/api-keys` | Admin | Create an API key |
| GET | `/admin/tenants/{tenantId}/api-keys` | Admin | List API keys for a tenant |
| DELETE | `/admin/tenants/{tenantId}/api-keys/{id}` | Admin | Delete API key |
| POST | `/admin/tenants/{tenantId}/api-keys/{id}/rotate` | Admin | Rotate API key (regenerate) |

### Admin — OAuth2 Clients (per tenant)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/admin/tenants/{tenantId}/clients` | Admin | Create an OAuth2 client |
| GET | `/admin/tenants/{tenantId}/clients` | Admin | List all clients for a tenant |
| GET | `/admin/tenants/{tenantId}/clients/{id}` | Admin | Get client by ID |
| PUT | `/admin/tenants/{tenantId}/clients/{id}` | Admin | Update client |
| DELETE | `/admin/tenants/{tenantId}/clients/{id}` | Admin | Delete client |

### Admin — System Stats

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/admin/stats` | Admin | System-wide statistics (tenants, users, roles, permissions, API keys, services, clients) |

---

## Identity.Web — Developer Portal (`http://localhost:5100`)

### OIDC Login Flow

1. User → **GET** `/` on Identity.Web (port 5100)
2. Identity.Web → **302** → Identity Server `/connect/authorize?client_id=web-ui&...` (port 5075)
3. Identity Server → **302** → `/Account/Login?ReturnUrl=...` (if not authenticated)
4. User submits email/password or chooses external provider
5. Identity Server validates, redirects to `/signin-oidc` with authorization code
6. Identity.Web exchanges code for tokens at `/connect/token`
7. User is now authenticated on the developer portal

### Developer Portal Pages

| URL | Description | Auth |
|-----|-------------|------|
| `/` | Home / landing page | Public |
| `/Status` | Health status page | Public |
| `/Developer/Index` | Developer portal hub | Authenticated |
| `/Developer/Dashboard` | Dashboard with stats & quick actions | Authenticated |
| `/Developer/ApiKeys` | API key management (CRUD) | Authenticated |
| `/Developer/Services` | External services management (CRUD + health) | Authenticated |
| `/Developer/Docs` | Integration documentation | Authenticated |
| `/Developer/Profile` | Edit user profile + change password | Authenticated |
| `/Developer/Securite` | Account security overview | Authenticated |
| `/Developer/Admin/Index` | Admin dashboard | Admin |
| `/Developer/Admin/Tenants` | Tenant list + CRUD | Admin |
| `/Developer/Admin/Tenants/{id}/Users` | User management per tenant | Admin |
| `/Developer/Admin/Tenants/{id}/Roles` | Role & permission management | Admin |
| `/Developer/Admin/Tenants/{id}/Clients` | OAuth2 client list | Admin |
| `/Developer/Admin/Tenants/{id}/ApiKeys` | API key management per tenant | Admin |

### Account Pages

| URL | Description |
|-----|-------------|
| `/Account/Login` | Login form (email/password) |
| `/Account/Logout` | Logout confirmation |
| `/Account/AccessDenied` | Access denied page |

### Other Pages

| URL | Description |
|-----|-------------|
| `/Securite` | Security policy page |
| `/Confidentialite` | Privacy policy page |
| `/ConditionsUtilisation` | Terms of service |
| `/Support` | Support / contact page |
| `/Error` | Global error page |

---

## Authentication

### Obtaining a Token

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password&username=user@example.com&password=MyPass@123&scope=openid+profile+email+roles+offline_access
```

### Using a Token

```http
GET /api/v1/profile
Authorization: Bearer eyJhbG...
```

### Using an API Key

```http
GET /api/v1/external-services
X-API-Key: dn_your_key_here
```

## Error Response

```json
{
  "message": "Description de l'erreur",
  "code": "SLUG_EXISTS"
}
```

| HTTP Status | Code | Description |
|-------------|------|-------------|
| 400 | `INVALID_OPERATION` | Validation error, invalid operation |
| 401 | `UNAUTHORIZED` | Missing or invalid authentication |
| 404 | `NOT_FOUND` | Resource not found |
| 409 | `SLUG_EXISTS` | Duplicate tenant/slug |
| 409 | `EMAIL_EXISTS` | Duplicate email |
| 500 | `INTERNAL_ERROR` | Unexpected server error |

## Headers

| Header | Description |
|--------|-------------|
| `Authorization: Bearer <token>` | JWT authentication |
| `X-API-Key: <key>` | API key authentication |
| `X-Internal-Key` | Internal service-to-service auth (Gateway↔Identity) |
| `X-Request-ID` | Request tracing (propagated by Gateway) |
| `ClientId` / `Oc-Client` | Rate limiting client identification |
| `X-Tenant-Id` | Tenant context for multi-tenant requests |

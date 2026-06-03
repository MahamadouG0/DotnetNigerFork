# Integration Guide — DotnetNiger.Identity

Complete guide for integrating client applications with the DotnetNiger Identity service.

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Client-Side JWT Configuration](#2-client-side-jwt-configuration)
3. [Authentication Endpoints](#3-authentication-endpoints)
4. [Authentication Flows](#4-authentication-flows)
5. [User, Role & Permission Management](#5-user-role--permission-management)
6. [Multi-Tenancy](#6-multi-tenancy)
7. [Complete cURL Examples](#7-complete-curl-examples)
8. [SMTP Configuration](#8-smtp-configuration)
9. [OAuth Provider Setup](#9-oauth-provider-setup)
10. [JWKS & Token Validation](#10-jwks--token-validation)
11. [Test Credentials](#11-test-credentials)

---

## 1. Prerequisites

- .NET 9.0+ SDK
- A client application (Web, Mobile, or API)
- NuGet package: `Microsoft.AspNetCore.Authentication.JwtBearer`

---

## 2. Client-Side JWT Configuration

### For ASP.NET Core Clients (e.g., Community service)

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = "http://localhost:5075";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
    options.MetadataAddress = "http://localhost:5075/.well-known/openid-configuration";
});

builder.Services.AddAuthorization();
```

The client automatically fetches public keys from the JWKS endpoint (`/.well-known/jwks`) to validate token signatures. No shared secret needed.

### For the Gateway (symmetric key validation)

The Gateway uses a shared symmetric key for JWT validation (must match Identity's configuration):

```json
{
  "Jwt": {
    "Key": "SharedSecretKeyMin32CharactersLong!",
    "Issuer": "DotnetNiger.Identity",
    "Audience": "DotnetNiger.Identity.Client"
  }
}
```

---

## 3. Authentication Endpoints

### Token Endpoint (OpenIddict)

| Method | Endpoint         | Content-Type                        | Description                         |
| ------ | ---------------- | ----------------------------------- | ----------------------------------- |
| POST   | `/connect/token` | `application/x-www-form-urlencoded` | Get JWT (password or refresh_token) |

### Auth Endpoints

| Method | Endpoint                         | Auth   | Description              |
| ------ | -------------------------------- | ------ | ------------------------ |
| POST   | `/api/v1/auth/register`          | —      | Create account           |
| POST   | `/api/v1/auth/confirm-email`     | —      | Confirm email (JSON)     |
| GET    | `/api/v1/auth/confirm-email`     | —      | Confirm email (query)    |
| POST   | `/api/v1/auth/resend-code`       | —      | Resend confirmation code |
| POST   | `/api/v1/auth/login`             | —      | JSON login               |
| POST   | `/api/v1/auth/logout`            | Bearer | Logout                   |
| GET    | `/api/v1/auth/userinfo`          | Bearer | Connected user info      |
| GET    | `/api/v1/auth/external-login`    | —      | OAuth provider redirect  |
| GET    | `/api/v1/auth/external-callback` | —      | OAuth callback           |

### Profile Endpoints

| Method | Endpoint          | Auth   | Description                                     |
| ------ | ----------------- | ------ | ----------------------------------------------- |
| GET    | `/api/v1/profile` | Bearer | Get own profile                                 |
| PUT    | `/api/v1/profile` | Bearer | Update profile (firstName, lastName, avatarUrl) |
| DELETE | `/api/v1/profile` | Bearer | Delete own account                              |

### User Management (Admin)

| Method | Endpoint                                        | Auth  | Description           |
| ------ | ----------------------------------------------- | ----- | --------------------- |
| POST   | `/api/v1/{tenantId}/users`                      | Admin | Create user in tenant |
| GET    | `/api/v1/{tenantId}/users`                      | Admin | List users in tenant  |
| GET    | `/api/v1/{tenantId}/users/{id}`                 | Admin | Get user by ID        |
| PUT    | `/api/v1/{tenantId}/users/{id}`                 | Admin | Update user           |
| DELETE | `/api/v1/{tenantId}/users/{id}`                 | Admin | Delete user           |
| POST   | `/api/v1/{tenantId}/users/{id}/change-password` | Admin | Change user password  |

### Role Management (Admin)

| Method | Endpoint                                           | Auth  | Description           |
| ------ | -------------------------------------------------- | ----- | --------------------- |
| POST   | `/api/v1/{tenantId}/roles`                         | Admin | Create role           |
| GET    | `/api/v1/{tenantId}/roles`                         | Admin | List roles            |
| PUT    | `/api/v1/{tenantId}/roles/{id}`                    | Admin | Update role           |
| DELETE | `/api/v1/{tenantId}/roles/{id}`                    | Admin | Delete role           |
| POST   | `/api/v1/{tenantId}/roles/{roleId}/users/{userId}` | Admin | Assign user to role   |
| DELETE | `/api/v1/{tenantId}/roles/{roleId}/users/{userId}` | Admin | Remove user from role |
| GET    | `/api/v1/{tenantId}/roles/user/{userId}`           | Admin | Get user roles        |

### Permission Management (Admin)

| Method | Endpoint                                 | Auth  | Description                     |
| ------ | ---------------------------------------- | ----- | ------------------------------- |
| POST   | `/api/v1/{tenantId}/permissions`         | Admin | Create permission               |
| GET    | `/api/v1/{tenantId}/permissions`         | Admin | List permissions                |
| GET    | `/api/v1/{tenantId}/permissions/grouped` | Admin | Permissions grouped by category |
| DELETE | `/api/v1/{tenantId}/permissions/{id}`    | Admin | Delete permission               |
| POST   | `/api/v1/{tenantId}/permissions/assign`  | Admin | Assign permissions to role      |

### Admin Endpoints

| Method | Endpoint                               | Auth  | Description              |
| ------ | -------------------------------------- | ----- | ------------------------ |
| GET    | `/api/v1/admin/stats`                  | Admin | Platform statistics      |
| CRUD   | `/api/v1/admin/tenants`                | Admin | Tenant management (CRUD) |
| GET    | `/api/v1/admin/tenants/by-slug/{slug}` | Admin | Get tenant by slug       |

### OpenID Connect Discovery

| Method | Endpoint                            | Description     |
| ------ | ----------------------------------- | --------------- |
| GET    | `/.well-known/openid-configuration` | OIDC metadata   |
| GET    | `/.well-known/jwks`                 | Public RSA keys |

### Diagnostics

| Method | Endpoint                     | Description  |
| ------ | ---------------------------- | ------------ |
| GET    | `/api/v1/diagnostics/health` | Health check |
| GET    | `/api/v1/diagnostics/ping`   | Ping         |

---

## 4. Authentication Flows

### 4.1 Registration with Email Confirmation

```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "MyPassword@123",
  "firstName": "Jean",
  "lastName": "Dupont"
}
```

Response:

```json
{
  "message": "Compte créé. Un code de confirmation vous a été envoyé par email.",
  "userId": "a1b2c3d4-...",
  "email": "user@example.com",
  "code": "A3F9K2"
}
```

> The `code` field is only returned in development (when SMTP is not configured). In production, the code is sent by email.

### 4.2 Email Confirmation (Two Methods)

**Method 1 — Clickable Link** (for web clients):

```
GET /api/v1/auth/confirm-email?email=user@example.com&code=A3F9K2
```

**Method 2 — JSON Body** (for API/mobile clients):

```http
POST /api/v1/auth/confirm-email
Content-Type: application/json

{
  "email": "user@example.com",
  "code": "A3F9K2"
}
```

### 4.3 Resend Confirmation Code

```http
POST /api/v1/auth/resend-code
Content-Type: application/json

{
  "email": "user@example.com"
}
```

### 4.4 Get JWT Token (Password Grant)

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password&username=user@example.com&password=MyPassword@123&scope=openid+profile+email+roles+offline_access&remember_me=true
```

Parameters:

| Parameter     | Required | Description                                                                            |
| ------------- | -------- | -------------------------------------------------------------------------------------- |
| `grant_type`  | Yes      | `password` or `refresh_token`                                                          |
| `username`    | Yes      | User email (for password grant)                                                        |
| `password`    | Yes      | User password (for password grant)                                                     |
| `scope`       | No       | Space-separated scopes: `openid`, `profile`, `email`, `roles`, `api`, `offline_access` |
| `remember_me` | No       | `true` = 7-day token, `false`/absent = 1-hour token                                    |

Response:

```json
{
  "access_token": "eyJhbG...",
  "token_type": "Bearer",
  "expires_in": 604799,
  "refresh_token": "eyJhbG..."
}
```

> `expires_in` = 3600 (1h) without `remember_me`, 604799 (7 days) with `remember_me=true`.
> `refresh_token` is only returned when `offline_access` scope is requested.

### 4.5 Refresh Token

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token&refresh_token=eyJhbG...&scope=openid+profile+email+roles+offline_access
```

Response: Same as password grant (new access_token + new refresh_token).

### 4.6 JSON Login (Credential Validation)

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "MyPassword@123",
  "rememberMe": true
}
```

Response:

```json
{
  "id": "guid-...",
  "email": "user@example.com",
  "firstName": "Jean",
  "lastName": "Dupont",
  "avatarUrl": null,
  "tenantId": "guid-...",
  "isActive": true,
  "roles": ["User"],
  "permissions": [],
  "rememberMe": true
}
```

> This endpoint validates credentials but does NOT return a JWT. Use `/connect/token` to obtain tokens.

### 4.7 Social Login (Google, Microsoft, GitHub)

**Step 1** — Redirect user to the provider:

```
GET /api/v1/auth/external-login?provider=Google
```

**Step 2** — Provider redirects to `/api/v1/auth/external-callback` with authorization code.

**Step 3** — Callback returns user info:

```json
{
  "id": "guid",
  "email": "user@gmail.com",
  "firstName": "Jean",
  "lastName": "Dupont",
  "avatarUrl": null,
  "tenantId": null,
  "isActive": true,
  "roles": ["User"],
  "permissions": []
}
```

> For browser flows, the callback should redirect to your frontend. Configure `returnUrl` in the initial call. Obtain JWT via `/connect/token` with `password` grant after social account creation.

---

## 5. User, Role & Permission Management

All admin endpoints are scoped to a tenant (`{tenantId}`) and require the `Admin` role.

### Create a User in a Tenant

```http
POST /api/v1/{tenantId}/users
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "email": "newuser@example.com",
  "password": "TempPass@123",
  "firstName": "New",
  "lastName": "User",
  "roles": ["User"]
}
```

### Assign Role to User

```http
POST /api/v1/{tenantId}/roles/{roleId}/users/{userId}
Authorization: Bearer <admin-token>
```

### Assign Permissions to Role

```http
POST /api/v1/{tenantId}/permissions/assign
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "roleId": "guid-...",
  "permissionIds": ["guid-1", "guid-2", "guid-3"]
}
```

---

## 6. Multi-Tenancy

### Tenant Context Resolution

The Identity service isolates data per tenant using two mechanisms:

1. **JWT Claim** — Authenticated requests read the `tenant_id` claim from the JWT
2. **X-Tenant-Id Header** — Public endpoints can specify tenant via this header

### Tenant Endpoints (Admin)

| Method | Endpoint                               | Description        |
| ------ | -------------------------------------- | ------------------ |
| POST   | `/api/v1/admin/tenants`                | Create tenant      |
| GET    | `/api/v1/admin/tenants`                | List all tenants   |
| GET    | `/api/v1/admin/tenants/{id}`           | Get tenant by ID   |
| GET    | `/api/v1/admin/tenants/by-slug/{slug}` | Get tenant by slug |
| PUT    | `/api/v1/admin/tenants/{id}`           | Update tenant      |
| DELETE | `/api/v1/admin/tenants/{id}`           | Delete tenant      |

---

## 7. Complete cURL Examples

```bash
#!/bin/bash

# ──────────────────────────────────────────────
# 1. Register a new account
# ──────────────────────────────────────────────
REG=$(curl -s -X POST http://localhost:5075/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@test.com","password":"Demo@123456","firstName":"Demo","lastName":"User"}')
echo "$REG" | jq .
CODE=$(echo "$REG" | jq -r '.code')

# ──────────────────────────────────────────────
# 2. Confirm email
# ──────────────────────────────────────────────
curl -s -X POST http://localhost:5075/api/v1/auth/confirm-email \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"demo@test.com\",\"code\":\"$CODE\"}" | jq .

# ──────────────────────────────────────────────
# 3. Login (validate credentials)
# ──────────────────────────────────────────────
curl -s -X POST http://localhost:5075/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@test.com","password":"Demo@123456","rememberMe":true}' | jq .

# ──────────────────────────────────────────────
# 4. Get JWT (with refresh token)
# ──────────────────────────────────────────────
TOKEN_RESP=$(curl -s -X POST http://localhost:5075/connect/token \
  -d "grant_type=password&username=demo@test.com&password=Demo@123456&scope=openid+profile+email+roles+offline_access&remember_me=true")
ACCESS_TOKEN=$(echo "$TOKEN_RESP" | jq -r '.access_token')
REFRESH_TOKEN=$(echo "$TOKEN_RESP" | jq -r '.refresh_token')
echo "Access Token: ${ACCESS_TOKEN:0:50}..."

# ──────────────────────────────────────────────
# 5. Call protected endpoints
# ──────────────────────────────────────────────
curl -s http://localhost:5075/api/v1/auth/userinfo \
  -H "Authorization: Bearer $ACCESS_TOKEN" | jq .

curl -s http://localhost:5075/api/v1/profile \
  -H "Authorization: Bearer $ACCESS_TOKEN" | jq .

# ──────────────────────────────────────────────
# 6. Refresh the token
# ──────────────────────────────────────────────
curl -s -X POST http://localhost:5075/connect/token \
  -d "grant_type=refresh_token&refresh_token=$REFRESH_TOKEN&scope=openid+profile+email+roles+offline_access" | jq .
```

---

## 8. SMTP Configuration

For production email sending (confirmation codes, etc.):

```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your@email.com",
    "Password": "your-password",
    "FromEmail": "noreply@dotnetniger.com",
    "FromName": "DotnetNiger",
    "AppBaseUrl": "http://localhost:5075"
  }
}
```

- If `Host` is empty, emails are logged to console (dev mode)
- `AppBaseUrl` is used to build confirmation links in emails

---

## 9. OAuth Provider Setup

### Google

1. Go to https://console.cloud.google.com/apis/credentials
2. Create OAuth client ID → Web application
3. Authorized redirect URI: `http://localhost:5075/signin-google`
4. Copy Client ID and Client Secret

### Microsoft

1. Go to https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps
2. New registration → Redirect URI: `http://localhost:5075/signin-microsoft`
3. Note Application (client) ID
4. Certificates & secrets → New client secret

### GitHub

1. Go to https://github.com/settings/developers
2. New OAuth App → Homepage: `http://localhost:5075`, Callback: `http://localhost:5075/signin-github`
3. Copy Client ID and Client Secret

### Activate

```json
{
  "Authentication": {
    "Google": { "ClientId": "...", "ClientSecret": "..." },
    "Microsoft": { "ClientId": "...", "ClientSecret": "..." },
    "GitHub": { "ClientId": "...", "ClientSecret": "..." }
  }
}
```

---

## 10. JWKS & Token Validation

JWKS (JSON Web Key Set) is an OIDC standard that exposes public keys for JWT signature verification.

**Endpoints:**

- `/.well-known/openid-configuration` — OIDC metadata
- `/.well-known/jwks` — Public RSA keys

**Advantages:**

- No need to share a secret key between services
- Key rotation possible without downtime
- Industry standard (OpenID Connect)

**Development mode:** OpenIddict uses ephemeral keys (change on every restart). For persistent keys, configure `AddSigningKey()` with a certificate in production.

---

## 11. Test Credentials

| Role         | Email                   | Password                    |
| ------------ | ----------------------- | --------------------------- |
| Super Admin  | `admin@dotnetniger.com` | `Admin@123456`              |
| Regular User | _(register via API)_    | _(set during registration)_ |

The super admin is created automatically by the database seeder on first run.

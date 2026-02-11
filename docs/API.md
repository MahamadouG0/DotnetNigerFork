# API

Base URL (dev): <http://localhost:5000>

## Auth

Tous les endpoints (sauf auth) exigent:

```
Authorization: Bearer <token>
```

Alternative via API key:

```
X-API-Key: <api_key>
```

Login example:

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!"
}
```

## Identity

- POST /api/v1/auth/register
- POST /api/v1/auth/login
- POST /api/v1/auth/forgot-password
- POST /api/v1/auth/reset-password
- POST /api/v1/auth/verify-email
- POST /api/v1/auth/request-email-verification
- POST /api/v1/tokens/refresh
- POST /api/v1/tokens/logout
- GET /api/v1/users/me
- PUT /api/v1/users/me
- GET /api/v1/users/me/avatar
- POST /api/v1/users/me/avatar
- DELETE /api/v1/users/me/avatar
- POST /api/v1/users/me/change-password
- POST /api/v1/users/me/change-email
- GET /api/v1/social-links
- POST /api/v1/social-links
- DELETE /api/v1/social-links/{id}
- GET /api/v1/api-keys
- POST /api/v1/api-keys
- POST /api/v1/api-keys/{apiKeyId}/rotate
- DELETE /api/v1/api-keys/{apiKeyId}
- POST /api/v1/api-keys/revoke-all
- GET /api/v1/integrations/ping (API key)
- GET /api/v1/roles
- POST /api/v1/roles
- DELETE /api/v1/roles/{id}
- POST /api/v1/roles/assign
- POST /api/v1/roles/remove
- GET /api/v1/roles/user/{userId}
- GET /api/v1/permissions
- POST /api/v1/permissions
- DELETE /api/v1/permissions/{id}
- POST /api/v1/permissions/assign
- POST /api/v1/permissions/remove
- GET /api/v1/permissions/role/{roleId}
- GET /api/v1/admin/users
- GET /api/v1/admin/users/{userId}
- PUT /api/v1/admin/users/{userId}/status
- GET /api/v1/admin/users/{userId}/login-history
- GET /api/v1/admin/api-keys
- POST /api/v1/admin/api-keys/{apiKeyId}/rotate
- DELETE /api/v1/admin/api-keys/{apiKeyId}
- POST /api/v1/admin/users/{userId}/api-keys/revoke-all
- GET /api/v1/admin/audit-logs
- GET /api/v1/diagnostics/ping
- GET /api/v1/diagnostics/health

## Gateway

- GET /health
- GET /swagger/v1/swagger.json
- GET /swagger-aggregated/v1/swagger.json (si active)
- GET /metrics (si active)

### API Keys

```bash
curl -X POST http://localhost:5000/api/v1/api-keys \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "cli-key",
    "expiresAt": "2026-06-01T00:00:00Z"
  }'

curl -X POST http://localhost:5000/api/v1/api-keys/revoke-all \
  -H "Authorization: Bearer <token>"

curl -X GET http://localhost:5000/api/v1/integrations/ping \
  -H "X-API-Key: <api_key>"
```

### Diagnostics

```bash
curl -X GET http://localhost:5000/api/v1/diagnostics/ping
curl -X GET http://localhost:5000/api/v1/diagnostics/health
```

### Logout

```bash
curl -X POST http://localhost:5000/api/v1/tokens/logout \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "<refresh_token>"
  }'
```

### Avatar (upload)

```bash
curl -X POST http://localhost:5075/api/v1/users/me/avatar \
  -H "Authorization: Bearer <token>" \
  -F "avatar=@/chemin/vers/avatar.png"
```

**Response:** `200 OK` - Profil utilisateur mis a jour

### Avatar (get)

```bash
curl -X GET http://localhost:5075/api/v1/users/me/avatar \
  -H "Authorization: Bearer <token>"
```

**Response:** `200 OK` - URL + metadonnees

```json
{
  "url": "http://localhost:5075/uploads/avatars/<id>/avatar.png",
  "hasAvatar": true,
  "provider": "Local",
  "exists": true,
  "sizeBytes": 120345,
  "contentType": "image/png",
  "fileName": "avatar.png"
}
```

### Avatar (delete)

```bash
curl -X DELETE http://localhost:5075/api/v1/users/me/avatar \
  -H "Authorization: Bearer <token>"
```

**Response:** `204 No Content`

### Admin Users (filters)

```http
GET /api/v1/admin/users?search=alex&isActive=true&emailConfirmed=true&role=Admin&createdFrom=2025-01-01&createdTo=2026-01-01&sortBy=createdAt&sortDirection=desc&skip=0&take=20
Authorization: Bearer <token>
```

### Admin API Keys (filters)

```http
GET /api/v1/admin/api-keys?search=cli&userId=<guid>&isActive=true&expired=false&createdFrom=2025-01-01&createdTo=2026-01-01&lastUsedFrom=2025-10-01&lastUsedTo=2026-02-01&sortBy=lastUsed&sortDirection=desc&skip=0&take=20
Authorization: Bearer <token>
```

### Admin API Keys (actions)

```http
POST /api/v1/admin/api-keys/{apiKeyId}/rotate
Authorization: Bearer <token>
```

```http
DELETE /api/v1/admin/api-keys/{apiKeyId}
Authorization: Bearer <token>
```

```http
POST /api/v1/admin/users/{userId}/api-keys/revoke-all
Authorization: Bearer <token>
```

### Admin Audit Logs

```http
GET /api/v1/admin/audit-logs?adminUserId=<guid>&action=api_key&targetType=api_key&createdFrom=2026-01-01&createdTo=2026-02-10&skip=0&take=20
Authorization: Bearer <token>
```
**Derniere mise a jour:** 10 Fevrier 2026
## Errors (standard)

- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 404 Not Found
- 429 Too Many Requests
- 500 Internal Server Error

````

#### Get Followers

```http
GET /api/users/{userId}/followers?skip=0&take=20
Authorization: Bearer <token>
````

**Response:** `200 OK`

### Feed

#### Get Personal Feed

```http
GET /api/feed?skip=0&take=20
Authorization: Bearer <token>
```

**Response:** `200 OK` - Posts des utilisateurs suivis

---

## 🏥 Gateway

### Health

#### Gateway Health

```http
GET /health
```

**Response:** `200 OK`

```json
{
  "status": "Healthy",
  "services": {
    "identity": "Healthy",
    "community": "Healthy"
  }
}
```

### Swagger

#### Aggregated Swagger

```http
GET /swagger/ui
GET /swagger/v1/swagger.json
```

### Metrics

#### Prometheus Metrics

```http
GET /metrics
```

**Response:** Format Prometheus

---

## ⚠️ Error Responses

### 400 Bad Request

```json
{
  "error": "Bad Request",
  "message": "Invalid input",
  "details": {
    "email": "Email is invalid"
  }
}
```

### 401 Unauthorized

```json
{
  "error": "Unauthorized",
  "message": "Invalid or missing token"
}
```

### 403 Forbidden

```json
{
  "error": "Forbidden",
  "message": "You don't have permission"
}
```

### 404 Not Found

```json
{
  "error": "Not Found",
  "message": "Resource not found"
}
```

### 429 Too Many Requests

```json
{
  "error": "Too Many Requests",
  "message": "Rate limit exceeded",
  "retryAfter": 60
}
```

### 500 Internal Server Error

```json
{
  "error": "Internal Server Error",
  "message": "An unexpected error occurred",
  "traceId": "0HN0E6HPVLF5Q:00000001"
}
```

---

## 📧 Email Configuration

### SMTP

```json
"Email": {
  "Enabled": true,
  "Provider": "smtp",
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "Username": "smtp_user",
    "Password": "smtp_password",
    "From": "no-reply@dotnetniger.com",
    "EnableSsl": true
  }
}
```

### SendGrid

```json
"Email": {
  "Enabled": true,
  "Provider": "sendgrid",
  "SendGrid": {
    "ApiKey": "SG.xxxxxx",
    "From": "no-reply@dotnetniger.com"
  }
}
```

### Mailgun

```json
"Email": {
  "Enabled": true,
  "Provider": "mailgun",
  "Mailgun": {
    "ApiKey": "key-xxxxxx",
    "Domain": "mg.dotnetniger.com",
    "From": "no-reply@dotnetniger.com"
  }
}
```

## 🧪 Testing avec cURL

### Register

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123!",
    "firstName": "Test",
    "lastName": "User"
  }'
```

### Login

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123!"
  }'
```

### Create Post

```bash
curl -X POST http://localhost:5000/api/posts \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Post",
    "content": "Test content",
    "tags": ["test"]
  }'
```

---

**Dernière mise à jour:** 29 Janvier 2026

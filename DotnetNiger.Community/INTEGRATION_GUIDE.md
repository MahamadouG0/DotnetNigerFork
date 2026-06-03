# Integration Guide — DotnetNiger.Community

Complete guide for integrating with the DotnetNiger Community API — posts, events, resources, comments, newsletters, member profiles, and administration.

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Architecture](#2-architecture)
3. [Authentication](#3-authentication)
4. [Posts](#4-posts)
5. [Events](#5-events)
6. [Resources](#6-resources)
7. [Comments](#7-comments)
8. [Member Profile](#8-member-profile)
9. [Newsletters](#9-newsletters)
10. [Search](#10-search)
11. [Admin](#11-admin)
12. [Response Format](#12-response-format)
13. [Complete cURL Examples](#13-complete-curl-examples)
14. [Error Handling](#14-error-handling)

---

## 1. Prerequisites

- .NET 9.0+ SDK
- [DotnetNiger.Identity](https://github.com/akaletekoffilevis/DotnetNiger) running on `http://localhost:5075`
- JWT Bearer token obtained from Identity (`/connect/token`)

---

## 2. Architecture

```
Client → Gateway (:5000) → Community (:5269 / :8082)
                              │
                              ▼
                         Identity (:5075)
                      (JWT validation, user mgmt)
```

In development, you can call Community directly on `http://localhost:5269`. In production, always go through the Gateway at `http://localhost:5000`.

---

## 3. Authentication

### Obtain a Token

```bash
TOKEN=$(curl -s -X POST http://localhost:5075/connect/token \
  -d "grant_type=password&username=admin@dotnetniger.com&password=Admin@123456&scope=openid+profile+email+roles+offline_access" \
  | jq -r '.access_token')
```

### Use the Token

```bash
curl -s http://localhost:5269/api/v1/admin/dashboard \
  -H "Authorization: Bearer $TOKEN"
```

### Token Claims

The JWT should contain:

- `sub` — User ID (Guid)
- `name` / `full_name` — Display name
- `email` — Email address
- `roles` — Array of role names
- `tenant_id` — Tenant identifier (for multi-tenant)
- `is_admin` / `is_super_admin` — Admin flags

---

## 4. Posts

### List Posts (Public)

```http
GET /api/v1/Posts?page=1&pageSize=10&category=tech&tag=dotnet
```

Optional query parameters: `page`, `pageSize`, `category`, `tag`, `search`, `sortBy`, `sortOrder`.

### Get Post by ID (Public)

```http
GET /api/v1/Posts/{id}
```

### Create Post (Authenticated)

```http
POST /api/v1/Posts
Authorization: Bearer <token>
Content-Type: application/json

{
  "title": "Getting Started with .NET 9",
  "content": "Full markdown content...",
  "excerpt": "Short description...",
  "coverImageUrl": "https://example.com/image.jpg",
  "category": "tech",
  "tags": ["dotnet", "csharp"],
  "isPublished": true
}
```

### Update Post (Authenticated — Owner or Admin)

```http
PUT /api/v1/Posts/{id}
Authorization: Bearer <token>
Content-Type: application/json

{
  "title": "Updated Title",
  "content": "Updated content..."
}
```

### Delete Post (Authenticated — Owner or Admin)

```http
DELETE /api/v1/Posts/{id}
Authorization: Bearer <token>
```

---

## 5. Events

### List Events (Public)

```http
GET /api/v1/Events?page=1&pageSize=10&upcoming=true
```

### Get Upcoming Events (Public)

```http
GET /api/v1/Events/upcoming
```

### Get Event by ID (Public)

```http
GET /api/v1/Events/{id}
```

### Create Event (Authenticated)

```http
POST /api/v1/Events
Authorization: Bearer <token>
Content-Type: application/json

{
  "title": "Dotnet Niger Meetup March 2026",
  "description": "Full description...",
  "startDate": "2026-03-20T18:00:00Z",
  "endDate": "2026-03-20T20:00:00Z",
  "location": "Virtual / Zoom",
  "maxAttendees": 100,
  "coverImageUrl": "https://example.com/event.jpg",
  "tags": ["meetup", "dotnet"],
  "isPublished": false
}
```

### Register for Event (Authenticated)

```http
POST /api/v1/Events/registrations
Authorization: Bearer <token>
Content-Type: application/json

{
  "eventId": "guid-...",
  "notes": "Looking forward to it!"
}
```

### Cancel Registration (Authenticated)

```http
DELETE /api/v1/Events/{eventId}/registrations
Authorization: Bearer <token>
```

### List Event Registrations (Authenticated — Event Owner or Admin)

```http
GET /api/v1/Events/{eventId}/registrations
Authorization: Bearer <token>
```

---

## 6. Resources

### List Resources (Public)

```http
GET /api/v1/Resources?page=1&pageSize=10&category=tutorial
```

### Get Resource by ID (Public)

```http
GET /api/v1/Resources/{id}
```

### Create Resource (Authenticated)

```http
POST /api/v1/Resources
Authorization: Bearer <token>
Content-Type: application/json

{
  "title": "Awesome .NET Library",
  "description": "Description...",
  "url": "https://github.com/example/library",
  "type": "github",
  "category": "tools",
  "tags": ["dotnet", "opensource"]
}
```

### Increment View Count (Public)

```http
POST /api/v1/Resources/{id}/views
```

---

## 7. Comments

### Get Comments for a Post (Public)

```http
GET /api/v1/Comments/post/{postId}
```

### Get Comments for an Event (Public)

```http
GET /api/v1/Comments/event/{eventId}
```

### Get Comment by ID (Public)

```http
GET /api/v1/Comments/{id}
```

### Create Comment (Authenticated)

```http
POST /api/v1/Comments
Authorization: Bearer <token>
Content-Type: application/json

{
  "postId": "guid-...",        // or eventId
  "content": "Great article!",
  "parentCommentId": null     // for replies
}
```

### Update Comment (Authenticated — Owner)

```http
PUT /api/v1/Comments/{id}
Authorization: Bearer <token>
Content-Type: application/json

{
  "content": "Updated comment..."
}
```

### Delete Comment (Authenticated — Owner)

```http
DELETE /api/v1/Comments/{id}
Authorization: Bearer <token>
```

Supports `?deleteAllReplies=true` to cascade delete.

---

## 8. Member Profile

### Get My Profile (Authenticated)

```http
GET /api/v1/me
Authorization: Bearer <token>
```

### Update My Profile (Authenticated)

```http
PUT /api/v1/me
Authorization: Bearer <token>
Content-Type: application/json

{
  "bio": "Full-stack developer passionate about .NET",
  "website": "https://example.com",
  "location": "Niamey, Niger",
  "skills": ["dotnet", "csharp", "azure"],
  "avatarUrl": "https://example.com/avatar.jpg"
}
```

### List Social Links (Authenticated)

```http
GET /api/v1/social-links
Authorization: Bearer <token>
```

### Add Social Link (Authenticated)

```http
POST /api/v1/social-links
Authorization: Bearer <token>
Content-Type: application/json

{
  "platform": "github",
  "url": "https://github.com/username"
}
```

### Delete Social Link (Authenticated)

```http
DELETE /api/v1/social-links/{id}
Authorization: Bearer <token>
```

---

## 9. Newsletters

### Subscribe (Public)

```http
POST /api/v1/newsletters/subscribe
Content-Type: application/json

{
  "email": "user@example.com",
  "firstName": "Jean",
  "tags": ["events", "news"]
}
```

### Quick Subscribe (Public)

```http
POST /api/v1/newsletters/quick-subscribe
Content-Type: application/json

{
  "email": "user@example.com"
}
```

### Verify Subscription (Public)

```http
POST /api/v1/newsletters/verify
Content-Type: application/json

{
  "email": "user@example.com",
  "token": "verification-token"
}
```

### Unsubscribe (Public)

```http
POST /api/v1/newsletters/unsubscribe
Content-Type: application/json

{
  "email": "user@example.com"
}
```

### List Subscriptions (Admin)

```http
GET /api/v1/newsletters/subscriptions
Authorization: Bearer <admin-token>
```

### Manage Subscription (Admin)

```http
PATCH /api/v1/newsletters/subscriptions/{id}
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "status": "active",
  "tags": ["events", "news", "jobs"]
}
```

---

## 10. Search

### Full-Text Search (Public)

```http
GET /api/v1/Search?q=dotnet&type=posts&page=1&pageSize=10
```

Parameters:

- `q` — Search query (required)
- `type` — Filter by type: `posts`, `events`, `resources`, or all (default)
- `page`, `pageSize` — Pagination

---

## 11. Admin

Admin endpoints require the `Admin` role in the JWT.

### Dashboard

```http
GET /api/v1/admin/dashboard
Authorization: Bearer <admin-token>
```

Returns: counts of users, posts, events, resources, recent activity.

### User Management

```http
GET /api/v1/admin/users                    # List users
GET /api/v1/admin/users/{id}               # Get user
PATCH /api/v1/admin/users/{id}/status      # Activate/deactivate

PATCH /api/v1/admin/users/{id}/status
Content-Type: application/json

{
  "isActive": false
}
```

### Role Management

```http
GET  /api/v1/admin/roles                   # List roles
POST /api/v1/admin/roles                   # Create role
POST /api/v1/admin/roles/{roleId}/permissions   # Assign permissions to role
POST /api/v1/admin/users/{userId}/roles         # Assign role to user
```

### Event Moderation

```http
PATCH /api/v1/admin/events/{id}/publish      # Publish event
PATCH /api/v1/admin/events/{id}/unpublish    # Unpublish event
```

---

## 12. Response Format

### Success Response

```json
{
  "success": true,
  "data": { ... }
}
```

### Error Response

```json
{
  "success": false,
  "message": "Description of the error"
}
```

### Paginated Response

```json
{
  "success": true,
  "data": {
    "items": [
      { "id": "guid-1", "title": "Post 1", ... },
      { "id": "guid-2", "title": "Post 2", ... }
    ],
    "totalCount": 25,
    "page": 1,
    "pageSize": 10,
    "totalPages": 3,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

---

## 13. Complete cURL Examples

```bash
#!/bin/bash

# ──────────────────────────────────────────────
# Configuration
# ──────────────────────────────────────────────
IDENTITY_URL="http://localhost:5075"
COMMUNITY_URL="http://localhost:5269"
GATEWAY_URL="http://localhost:5000"

# ──────────────────────────────────────────────
# 1. Get admin token from Identity
# ──────────────────────────────────────────────
TOKEN=$(curl -s -X POST "$IDENTITY_URL/connect/token" \
  -d "grant_type=password&username=admin@dotnetniger.com&password=Admin@123456&scope=openid+profile+email+roles+offline_access" \
  | jq -r '.access_token')
echo "Token obtained: ${TOKEN:0:50}..."

# ──────────────────────────────────────────────
# 2. Create a post (via Gateway)
# ──────────────────────────────────────────────
curl -s -X POST "$GATEWAY_URL/api/posts" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Hello .NET Niger!",
    "content": "Welcome to our community platform.",
    "excerpt": "First post",
    "category": "announcement",
    "tags": ["welcome"],
    "isPublished": true
  }' | jq .

# ──────────────────────────────────────────────
# 3. List public posts (no auth needed)
# ──────────────────────────────────────────────
curl -s "$GATEWAY_URL/api/posts?page=1&pageSize=5" | jq .

# ──────────────────────────────────────────────
# 4. Create an event
# ──────────────────────────────────────────────
curl -s -X POST "$GATEWAY_URL/api/events" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Community Meetup April 2026",
    "description": "Monthly community meetup",
    "startDate": "2026-04-15T18:00:00Z",
    "endDate": "2026-04-15T20:00:00Z",
    "location": "Virtual",
    "maxAttendees": 50,
    "isPublished": true
  }' | jq .

# ──────────────────────────────────────────────
# 5. Search
# ──────────────────────────────────────────────
curl -s "$GATEWAY_URL/api/search?q=.NET&type=all" | jq .

# ──────────────────────────────────────────────
# 6. Admin dashboard
# ──────────────────────────────────────────────
curl -s "$GATEWAY_URL/api/community/admin/dashboard" \
  -H "Authorization: Bearer $TOKEN" | jq .

# ──────────────────────────────────────────────
# 7. Check health (via Gateway)
# ──────────────────────────────────────────────
curl -s "$GATEWAY_URL/health/downstream" | jq .
```

---

## 14. Error Handling

The Gateway includes an `ErrorHandlingMiddleware` that catches unhandled exceptions and returns structured problem+json responses:

```json
{
  "type": "https://httpstatuses.io/500",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred",
  "instance": "/api/v1/posts"
}
```

### HTTP Status Codes

| Code | Description                          |
| ---- | ------------------------------------ |
| 200  | Success                              |
| 201  | Created                              |
| 400  | Bad Request (validation error)       |
| 401  | Unauthorized (missing/invalid token) |
| 403  | Forbidden (insufficient permissions) |
| 404  | Not Found                            |
| 429  | Too Many Requests (rate limited)     |
| 500  | Internal Server Error                |

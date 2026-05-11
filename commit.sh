#!/usr/bin/env bash
set -euo pipefail

echo "═══════════════════════════════════════════════════════════════"
echo "  DotnetNiger Identity — Commit Script"
echo "  Review the changes below before committing."
echo "═══════════════════════════════════════════════════════════════"
echo ""

# ──────────────────────────────────────────────────────────────────────
# 1. Show full git status for verification
# ──────────────────────────────────────────────────────────────────────
echo ">>> git status"
echo "───────────────────────────────────────────────────────────────"
git status
echo ""

echo ">>> git diff --stat"
echo "───────────────────────────────────────────────────────────────"
git diff --stat
echo ""

# ──────────────────────────────────────────────────────────────────────
# 2. Confirm with user before proceeding
# ──────────────────────────────────────────────────────────────────────
read -r -p "Proceed with committing? [y/N] " reply
if [[ ! "$reply" =~ ^[Yy]$ ]]; then
    echo "Aborted."
    exit 1
fi

# ──────────────────────────────────────────────────────────────────────
# 3. Commit 1 — Refactor: simplified clean architecture with OpenIddict
#    (deletes old bloated layers, adds new minimal structure)
# ──────────────────────────────────────────────────────────────────────
echo ""
echo ">>> Commit 1: Clean architecture refactor with OpenIddict + multi-tenant"
echo "───────────────────────────────────────────────────────────────"

git add \
  DotnetNiger.Identity/DotnetNiger.Identity.csproj \
  DotnetNiger.Identity/DotnetNiger.Identity.sln \
  DotnetNiger.Identity/Program.cs \
  DotnetNiger.Identity/GlobalUsings.cs \
  DotnetNiger.Identity/Api/ServiceExtensions.cs \
  DotnetNiger.Identity/Api/Middleware/ErrorHandlingMiddleware.cs \
  DotnetNiger.Identity/Api/Middleware/TenantResolutionMiddleware.cs \
  DotnetNiger.Identity/Api/Controllers/AuthController.cs \
  DotnetNiger.Identity/Api/Controllers/AdminController.cs \
  DotnetNiger.Identity/Api/Controllers/PermissionsController.cs \
  DotnetNiger.Identity/Api/Controllers/RolesController.cs \
  DotnetNiger.Identity/Api/Controllers/UsersController.cs \
  DotnetNiger.Identity/Api/Controllers/ProfileController.cs \
  DotnetNiger.Identity/Api/Controllers/TenantsController.cs \
  DotnetNiger.Identity/Application/DTOs/AuthRequests.cs \
  DotnetNiger.Identity/Application/DTOs/AuthResponses.cs \
  DotnetNiger.Identity/Application/DTOs/Common.cs \
  DotnetNiger.Identity/Application/DTOs/PermissionRequests.cs \
  DotnetNiger.Identity/Application/DTOs/PermissionResponses.cs \
  DotnetNiger.Identity/Application/DTOs/RoleRequests.cs \
  DotnetNiger.Identity/Application/DTOs/RoleResponses.cs \
  DotnetNiger.Identity/Application/DTOs/TenantRequests.cs \
  DotnetNiger.Identity/Application/DTOs/TenantResponses.cs \
  DotnetNiger.Identity/Application/DTOs/UserRequests.cs \
  DotnetNiger.Identity/Application/DTOs/UserResponses.cs \
  DotnetNiger.Identity/Application/Services/AuthService.cs \
  DotnetNiger.Identity/Application/Services/AdminService.cs \
  DotnetNiger.Identity/Application/Services/PermissionService.cs \
  DotnetNiger.Identity/Application/Services/RoleService.cs \
  DotnetNiger.Identity/Application/Services/UserService.cs \
  DotnetNiger.Identity/Application/Services/TenantService.cs \
  DotnetNiger.Identity/Application/Validators.cs \
  DotnetNiger.Identity/Domain/Entities/ApplicationUser.cs \
  DotnetNiger.Identity/Domain/Entities/ApplicationRole.cs \
  DotnetNiger.Identity/Domain/Entities/Permission.cs \
  DotnetNiger.Identity/Domain/Entities/Tenant.cs \
  DotnetNiger.Identity/Infrastructure/IdentityDbContext.cs \
  DotnetNiger.Identity/Infrastructure/DesignTimeDbContextFactory.cs \
  DotnetNiger.Identity/Infrastructure/DbSeeder.cs \
  DotnetNiger.Identity/Infrastructure/TenantResolutionService.cs \
  DotnetNiger.Identity/Infrastructure/ClaimsTransformer.cs \
  DotnetNiger.Identity/Infrastructure/EmailSender.cs \
  DotnetNiger.Identity/appsettings.json \
  DotnetNiger.Identity/appsettings.Development.json \
  DotnetNiger.Identity/Properties/launchSettings.json

# Deleted files from old architecture
git add \
  -A DotnetNiger.Identity/Application/Abstractions/ \
  -A DotnetNiger.Identity/Application/Exceptions/ \
  -A DotnetNiger.Identity/Application/Mappers/ \
  -A DotnetNiger.Identity/Application/Validators/ \
  -A DotnetNiger.Identity/Application/DTOs/Requests/ \
  -A DotnetNiger.Identity/Application/DTOs/Responses/ \
  -A DotnetNiger.Identity/Application/Services/Interfaces/ \
  -A DotnetNiger.Identity/Api/Extensions/ \
  -A DotnetNiger.Identity/Api/Filters/ \
  -A DotnetNiger.Identity/Api/Middleware/JwtMiddleware.cs \
  -A DotnetNiger.Identity/Api/Middleware/RequestLoggingMiddleware.cs \
  -A DotnetNiger.Identity/Api/Middleware/EndpointLatencyMetricsMiddleware.cs \
  -A DotnetNiger.Identity/Api/Controllers/ApiControllerBase.cs \
  -A DotnetNiger.Identity/Api/Controllers/BootstrapController.cs \
  -A DotnetNiger.Identity/Api/Controllers/SocialLinksController.cs \
  -A DotnetNiger.Identity/Domain/Entities/AccountDeletionRequest.cs \
  -A DotnetNiger.Identity/Domain/Entities/AdminActionLog.cs \
  -A DotnetNiger.Identity/Domain/Entities/ApiKey.cs \
  -A DotnetNiger.Identity/Domain/Entities/AppSetting.cs \
  -A DotnetNiger.Identity/Domain/Entities/LoginHistory.cs \
  -A DotnetNiger.Identity/Domain/Entities/RefreshToken.cs \
  -A DotnetNiger.Identity/Domain/Entities/Role.cs \
  -A DotnetNiger.Identity/Domain/Entities/RolePermission.cs \
  -A DotnetNiger.Identity/Domain/Entities/SocialLink.cs \
  -A DotnetNiger.Identity/Domain/Enums/ \
  -A DotnetNiger.Identity/Domain/Interfaces/ \
  -A DotnetNiger.Identity/Infrastructure/Repositories/ \
  -A DotnetNiger.Identity/Infrastructure/Caching/ \
  -A DotnetNiger.Identity/Infrastructure/External/ \
  -A DotnetNiger.Identity/Infrastructure/Security/ \
  -A DotnetNiger.Identity/Infrastructure/Data/ \
  -A DotnetNiger.Identity/Migrations/ \
  -A DotnetNiger.Identity/README.md \
  -A DotnetNiger.Identity/DotnetNiger.Identity.http \
  -A DotnetNiger.Identity/.dockerignore \
  -A DotnetNiger.Identity/Properties/serviceDependencies.json \
  -A DotnetNiger.Identity/Properties/serviceDependencies.local.json

git commit -m "refactor: replace bloated architecture with minimal OpenIddict-based multi-tenant Identity service

- Remove all repository layers, service interfaces, DTO folders, validators,
  caching, file upload, social links, API keys, migrations, and old JWT logic
- Add OpenIddict (OAuth2/OIDC) with password + refresh token + social login flows
- Add multi-tenant isolation via EF Core query filters on TenantId
- Add email confirmation flow (MailKit SMTP + confirmation code)
- Add role/permission management with tenant-scoped Admin role
- Keep controllers under 200 lines, services minimal, zero interfaces" \
  || echo "Commit 1 skipped (nothing to commit or error)"

# ──────────────────────────────────────────────────────────────────────
# 4. Commit 2 — Dockerfile + docker-compose fix + health endpoint
# ──────────────────────────────────────────────────────────────────────
echo ""
echo ">>> Commit 2: Dockerfile, health endpoint, and docker-compose fix"
echo "───────────────────────────────────────────────────────────────"

git add \
  DotnetNiger.Identity/Dockerfile \
  DotnetNiger.Identity/Api/Controllers/DiagnosticsController.cs \
  docker-compose.yml

git commit -m "feat: add Dockerfile, health endpoint, and fix docker-compose connection string

- Create multi-stage Dockerfile (net9.0, port 8081, curl healthcheck)
- Add /health (minimal API) and /api/v1/diagnostics/health endpoints
- Fix docker-compose env var ConnectionStrings__DotnetNigerDb → DefaultConnection" \
  || echo "Commit 2 skipped (nothing to commit or error)"

# ──────────────────────────────────────────────────────────────────────
# 5. Commit 3 — Remember Me feature
# ──────────────────────────────────────────────────────────────────────
echo ""
echo ">>> Commit 3: Remember Me on login"
echo "───────────────────────────────────────────────────────────────"

git add \
  DotnetNiger.Identity/Api/Controllers/AuthController.cs \
  DotnetNiger.Identity/Application/DTOs/AuthRequests.cs \
  DotnetNiger.Identity/Application/DTOs/AuthResponses.cs

git commit -m "feat: add rememberMe to login — 7-day token when remember_me=true

- Add RememberMe bool to LoginRequest and UserInfoResponse
- TokenExchange sets AccessTokenLifetime to 7 days (vs 1h) when remember_me=true
- /api/v1/auth/login returns rememberMe flag in response" \
  || echo "Commit 3 skipped (nothing to commit or error)"

# ──────────────────────────────────────────────────────────────────────
# 6. Commit 4 — Docs
# ──────────────────────────────────────────────────────────────────────
echo ""
echo ">>> Commit 4: Update integration guide"
echo "───────────────────────────────────────────────────────────────"

git add \
  DotnetNiger.Identity/INTEGRATION_GUIDE.md

git commit -m "docs: update integration guide with refresh token, rememberMe, and health endpoints

- Add refresh_token grant type documentation
- Add remember_me parameter to /connect/token and /api/v1/auth/login
- Add health endpoint section
- Update cURL example with full refresh token flow" \
  || echo "Commit 4 skipped (nothing to commit or error)"

# ──────────────────────────────────────────────────────────────────────
# Summary
# ──────────────────────────────────────────────────────────────────────
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Done! Run 'git log --oneline' to verify commits."
echo "  Run 'git push' when ready."
echo "═══════════════════════════════════════════════════════════════"

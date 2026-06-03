using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using DotnetNiger.Identity.Application.Exceptions;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using DotnetNiger.Identity.Application.DTOs;

namespace DotnetNiger.Identity.Application.Services;

public class TenantService
{
    private readonly IdentityDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly TenantApiKeyService _apiKeyService;
    private readonly string _adminPassword;

    public TenantService(IdentityDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOpenIddictApplicationManager applicationManager,
        TenantApiKeyService apiKeyService,
        IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _applicationManager = applicationManager;
        _apiKeyService = apiKeyService;
        _adminPassword = configuration["Admin:DefaultPassword"] ?? throw new InvalidOperationException("Admin:DefaultPassword must be set via user-secrets or environment variable");
    }

    public async Task<TenantResponse> CreateAsync(CreateTenantRequest request)
    {
        if (await _db.Tenants.AnyAsync(t => t.Slug == request.Slug.ToLowerInvariant()))
            throw new SlugAlreadyExistsException(request.Slug);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = request.Slug.ToLowerInvariant(),
            Description = request.Description,
            IsActive = true
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        var adminRole = new ApplicationRole
        {
            Name = "Admin",
            NormalizedName = "ADMIN",
            TenantId = tenant.Id,
            Description = $"Administrateur de {tenant.Name}"
        };
        await _roleManager.CreateAsync(adminRole);

        var adminUser = new ApplicationUser
        {
            UserName = $"admin@{tenant.Slug}.dotnetniger.com",
            Email = $"admin@{tenant.Slug}.dotnetniger.com",
            FirstName = "Admin",
            LastName = tenant.Name,
            TenantId = tenant.Id,
            IsActive = true,
            EmailConfirmed = true
        };
        var result = await _userManager.CreateAsync(adminUser, _adminPassword);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(adminUser, "Admin");
        }

        return MapToResponse(tenant);
    }

    public async Task<PaginatedResponse<TenantResponse>> GetAllAsync(PaginationQuery pagination)
    {
        var query = _db.Tenants.OrderBy(t => t.Name);
        var total = await query.CountAsync();
        var tenants = await query
            .Skip((pagination.EnsurePage - 1) * pagination.EnsurePageSize)
            .Take(pagination.EnsurePageSize)
            .ToListAsync();
        return new PaginatedResponse<TenantResponse>(
            tenants.Select(t => new TenantResponse(t.Id, t.Name, t.Slug, t.Description, t.IsActive, t.CreatedAt)).ToList(),
            total, pagination.EnsurePage, pagination.EnsurePageSize);
    }

    public async Task<TenantResponse?> GetByIdAsync(Guid id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        return tenant == null ? null : MapToResponse(tenant);
    }

    public async Task<TenantResponse?> GetBySlugAsync(string slug)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug);
        return tenant == null ? null : MapToResponse(tenant);
    }

    public async Task<TenantResponse> UpdateAsync(Guid id, UpdateTenantRequest request)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) throw new KeyNotFoundException("Tenant non trouvé");

        if (request.Name != null) tenant.Name = request.Name;
        if (request.Description != null) tenant.Description = request.Description;
        if (request.IsActive.HasValue) tenant.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();
        return MapToResponse(tenant);
    }

    public async Task DeleteAsync(Guid id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant != null)
        {
            _db.Tenants.Remove(tenant);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<RegisterTenantResponse> RegisterTenantAsync(RegisterTenantRequest request)
    {
        if (await _db.Tenants.AnyAsync(t => t.Slug == request.Slug.ToLowerInvariant()))
            throw new SlugAlreadyExistsException(request.Slug);

        if (await _userManager.FindByEmailAsync(request.AdminEmail) != null)
            throw new EmailAlreadyExistsException(request.AdminEmail);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.CompanyName,
            Slug = request.Slug.ToLowerInvariant(),
            IsActive = true
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        var adminRole = new ApplicationRole
        {
            Name = "Admin",
            NormalizedName = "ADMIN",
            TenantId = tenant.Id,
            Description = $"Administrateur de {tenant.Name}"
        };
        var userRole = new ApplicationRole
        {
            Name = "User",
            NormalizedName = "USER",
            TenantId = tenant.Id,
            Description = "Utilisateur standard"
        };
        await _roleManager.CreateAsync(adminRole);
        await _roleManager.CreateAsync(userRole);

        var adminUser = new ApplicationUser
        {
            UserName = request.AdminEmail,
            Email = request.AdminEmail,
            FirstName = request.AdminFirstName,
            LastName = request.AdminLastName,
            TenantId = tenant.Id,
            IsActive = true,
            EmailConfirmed = true
        };
        var result = await _userManager.CreateAsync(adminUser, request.AdminPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Échec création admin : {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await _userManager.AddToRoleAsync(adminUser, "Admin");

        var (clientId, clientSecret) = await CreateDefaultClientAsync(tenant);
        var apiKey = await CreateDefaultApiKeyAsync(tenant);

        return new RegisterTenantResponse(
            tenant.Id, tenant.Name, tenant.Slug, request.AdminEmail,
            clientId, clientSecret, apiKey.Key.Id, apiKey.PrivateKey);
    }

    private async Task<(string clientId, string clientSecret)> CreateDefaultClientAsync(Tenant tenant)
    {
        var clientId = $"app_{Guid.NewGuid():N}";
        var clientSecret = GenerateSecret();
        var clientSecretHash = HashSecret(clientSecret);

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = $"{tenant.Name} — Application par défaut",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            ApplicationType = OpenIddictConstants.ApplicationTypes.Web,
        };

        descriptor.Permissions.Add("ep:token");
        descriptor.Permissions.Add("ep:authorization");
        descriptor.Permissions.Add("ep:logout");
        descriptor.Permissions.Add("ep:userinfo");
        descriptor.Permissions.Add("gt:authorization_code");
        descriptor.Permissions.Add("gt:password");
        descriptor.Permissions.Add("gt:refresh_token");
        descriptor.Permissions.Add("gt:client_credentials");
        descriptor.Permissions.Add("scp:openid");
        descriptor.Permissions.Add("scp:email");
        descriptor.Permissions.Add("scp:profile");
        descriptor.Permissions.Add("scp:roles");
        descriptor.Permissions.Add("scp:offline_access");
        descriptor.Permissions.Add("scp:api");

        var app = await _applicationManager.CreateAsync(descriptor)
            ?? throw new InvalidOperationException("Échec création application OpenIddict");

        var applicationId = await _applicationManager.GetIdAsync(app);

        var tenantClient = new TenantClient
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ApplicationId = applicationId!,
            ClientId = clientId,
            ClientSecretHash = clientSecretHash,
            ClientName = $"{tenant.Name} — Application par défaut",
            RedirectUris = "[]",
            PostLogoutRedirectUris = "[]",
            AllowedGrantTypes = JsonSerializer.Serialize(new[]
            {
                "authorization_code", "password", "refresh_token", "client_credentials"
            }),
            IsActive = true,
        };

        _db.TenantClients.Add(tenantClient);
        await _db.SaveChangesAsync();

        return (clientId, clientSecret);
    }

    private async Task<TenantApiKeyCreatedResponse> CreateDefaultApiKeyAsync(Tenant tenant)
    {
        return await _apiKeyService.CreateApiKeyAsync(tenant.Id, new CreateTenantApiKeyRequest(
            $"{tenant.Name} — Clé API par défaut",
            "[\"api\"]", null));
    }

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    private static string HashSecret(string secret)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret));
        return Convert.ToBase64String(bytes);
    }

    private static TenantResponse MapToResponse(Tenant t) =>
        new(t.Id, t.Name, t.Slug, t.Description, t.IsActive, t.CreatedAt);
}

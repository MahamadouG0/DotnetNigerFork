using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using DotnetNiger.Identity.Domain.Entities;

namespace DotnetNiger.Identity.Infrastructure;

public class OAuthTestUser
{
    public const string Email = "oauthuser@test.com";
    public const string Password = "Test@12345";
    public const string Username = "oauthuser@test.com";
}

/// <summary>
/// Initialise la base de données avec les données de base :
/// - Tenant plateforme
/// - Rôle Admin + User pour la plateforme
/// - Compte admin par défaut
/// - Permissions standards
/// </summary>
public class DbSeeder
{
    public static async Task SeedAsync(IdentityDbContext db, UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager, TenantContext tenantContext, string adminPassword,
        IOpenIddictApplicationManager appManager)
    {
        if (await db.Tenants.AnyAsync()) return;

        var platformTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Plateforme",
            Slug = "platform",
            Description = "Tenant de la plateforme DotnetNiger",
            IsActive = true
        };
        db.Tenants.Add(platformTenant);
        await db.SaveChangesAsync();

        tenantContext.TenantId = platformTenant.Id;

        var adminRole = new ApplicationRole
        {
            Name = "Admin",
            NormalizedName = "ADMIN",
            TenantId = platformTenant.Id,
            Description = "Administrateur de la plateforme"
        };
        var userRole = new ApplicationRole
        {
            Name = "User",
            NormalizedName = "USER",
            TenantId = platformTenant.Id,
            Description = "Utilisateur standard"
        };
        await roleManager.CreateAsync(adminRole);
        await roleManager.CreateAsync(userRole);

        var permissions = new List<Permission>
        {
            new() { Id = Guid.NewGuid(), TenantId = platformTenant.Id, Name = "user.read", Category = "User" },
            new() { Id = Guid.NewGuid(), TenantId = platformTenant.Id, Name = "user.write", Category = "User" },
            new() { Id = Guid.NewGuid(), TenantId = platformTenant.Id, Name = "role.manage", Category = "Role" },
            new() { Id = Guid.NewGuid(), TenantId = platformTenant.Id, Name = "permission.manage", Category = "Permission" },
            new() { Id = Guid.NewGuid(), TenantId = platformTenant.Id, Name = "tenant.manage", Category = "Tenant" },
        };
        db.Permissions.AddRange(permissions);
        await db.SaveChangesAsync();

        var adminUser = new ApplicationUser
        {
            UserName = "admin@dotnetniger.com",
            Email = "admin@dotnetniger.com",
            FirstName = "Admin",
            LastName = "Plateforme",
            TenantId = platformTenant.Id,
            IsActive = true,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        await CreateWebUiClientAsync(appManager);
        await CreateTestIdentityClientAsync(appManager);
        await CreateTestClientAsync(db, appManager, userManager, platformTenant.Id);
    }

    private static async Task CreateTestIdentityClientAsync(IOpenIddictApplicationManager appManager)
    {
        var existing = await appManager.FindByClientIdAsync("test-identity");
        if (existing != null) return;

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "test-identity",
            ClientSecret = null,
            DisplayName = "TestIdentity — Application de test OIDC",
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ApplicationType = OpenIddictConstants.ApplicationTypes.Web,
        };

        descriptor.RedirectUris.Add(new Uri("http://localhost:5200/signin-oidc"));
        descriptor.PostLogoutRedirectUris.Add(new Uri("http://localhost:5200/"));
        descriptor.PostLogoutRedirectUris.Add(new Uri("http://localhost:5200/signout-callback-oidc"));
        descriptor.Permissions.Add("ep:token");
        descriptor.Permissions.Add("ep:authorization");
        descriptor.Permissions.Add("ep:logout");
        descriptor.Permissions.Add("ep:userinfo");
        descriptor.Permissions.Add("gt:authorization_code");
        descriptor.Permissions.Add("gt:refresh_token");
        descriptor.Permissions.Add("rst:code");
        descriptor.Permissions.Add("scp:openid");
        descriptor.Permissions.Add("scp:email");
        descriptor.Permissions.Add("scp:profile");
        descriptor.Permissions.Add("scp:roles");
        descriptor.Permissions.Add("scp:offline_access");
        await appManager.CreateAsync(descriptor);
    }

    private static async Task CreateWebUiClientAsync(IOpenIddictApplicationManager appManager)
    {
        var existing = await appManager.FindByClientIdAsync("web-ui");
        if (existing != null) return;

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "web-ui",
            ClientSecret = null,
            DisplayName = "Web UI — Portail développeur",
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ApplicationType = OpenIddictConstants.ApplicationTypes.Web,
        };

        descriptor.RedirectUris.Add(new Uri("http://localhost:5100/signin-oidc"));
        descriptor.PostLogoutRedirectUris.Add(new Uri("http://localhost:5100/"));
        descriptor.PostLogoutRedirectUris.Add(new Uri("http://localhost:5100/signout-callback-oidc"));
        descriptor.Permissions.Add("ep:token");
        descriptor.Permissions.Add("ep:authorization");
        descriptor.Permissions.Add("ep:logout");
        descriptor.Permissions.Add("ep:userinfo");
        descriptor.Permissions.Add("gt:authorization_code");
        descriptor.Permissions.Add("gt:refresh_token");
        descriptor.Permissions.Add("rst:code");
        descriptor.Permissions.Add("scp:openid");
        descriptor.Permissions.Add("scp:email");
        descriptor.Permissions.Add("scp:profile");
        descriptor.Permissions.Add("scp:roles");
        descriptor.Permissions.Add("scp:offline_access");
        await appManager.CreateAsync(descriptor);
    }

    private static async Task CreateTestClientAsync(IdentityDbContext db, IOpenIddictApplicationManager appManager, UserManager<ApplicationUser> userManager, Guid tenantId)
    {
        var existing = await appManager.FindByClientIdAsync("test-client");
        if (existing != null) return;

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "test-client",
            ClientSecret = "test-secret",
            DisplayName = "Test Client — Tests OAuth2",
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
        };

        descriptor.Permissions.Add("ep:token");
        descriptor.Permissions.Add("ep:authorization");
        descriptor.Permissions.Add("ep:logout");
        descriptor.Permissions.Add("ep:userinfo");
        descriptor.Permissions.Add("gt:password");
        descriptor.Permissions.Add("gt:refresh_token");
        descriptor.Permissions.Add("gt:client_credentials");
        descriptor.Permissions.Add("scp:openid");
        descriptor.Permissions.Add("scp:email");
        descriptor.Permissions.Add("scp:profile");
        descriptor.Permissions.Add("scp:roles");
        descriptor.Permissions.Add("scp:offline_access");
        descriptor.Permissions.Add("scp:api");
        var app = await appManager.CreateAsync(descriptor);
        var applicationId = await appManager.GetIdAsync(app);

        var tenantClient = new TenantClient
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ApplicationId = applicationId!,
            ClientId = "test-client",
            ClientSecretHash = HashSecret("test-secret"),
            ClientName = "Test Client",
            Description = "Client de test pour les tests OAuth2",
            RedirectUris = "[]",
            PostLogoutRedirectUris = "[]",
            AllowedGrantTypes = JsonSerializer.Serialize(new[] { "password", "refresh_token", "client_credentials" }),
            IsActive = true,
        };
        db.TenantClients.Add(tenantClient);
        await db.SaveChangesAsync();

        var testUser = await userManager.FindByEmailAsync(OAuthTestUser.Email);
        if (testUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = OAuthTestUser.Username,
                Email = OAuthTestUser.Email,
                FirstName = "OAuth",
                LastName = "Test",
                TenantId = tenantId,
                IsActive = true,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, OAuthTestUser.Password);
        }
    }

    private static string HashSecret(string secret)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret));
        return Convert.ToBase64String(bytes);
    }
}

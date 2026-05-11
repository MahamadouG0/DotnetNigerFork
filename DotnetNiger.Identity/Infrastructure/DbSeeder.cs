using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DotnetNiger.Identity.Domain.Entities;

namespace DotnetNiger.Identity.Infrastructure;

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
        RoleManager<ApplicationRole> roleManager, TenantContext tenantContext)
    {
        await db.Database.EnsureCreatedAsync();

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
        var result = await userManager.CreateAsync(adminUser, "Admin@123456");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

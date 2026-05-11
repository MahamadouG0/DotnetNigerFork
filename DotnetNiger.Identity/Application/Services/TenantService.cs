using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using DotnetNiger.Identity.Application.DTOs;

namespace DotnetNiger.Identity.Application.Services;

public class TenantService
{
    private readonly IdentityDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public TenantService(IdentityDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<TenantResponse> CreateAsync(CreateTenantRequest request)
    {
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
        var result = await _userManager.CreateAsync(adminUser, "Admin@123456");
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(adminUser, "Admin");
        }

        return MapToResponse(tenant);
    }

    public async Task<List<TenantResponse>> GetAllAsync()
    {
        var tenants = await _db.Tenants.OrderBy(t => t.Name).ToListAsync();
        return tenants.Select(t => new TenantResponse(
            t.Id, t.Name, t.Slug, t.Description, t.IsActive, t.CreatedAt)).ToList();
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

    private static TenantResponse MapToResponse(Tenant t) =>
        new(t.Id, t.Name, t.Slug, t.Description, t.IsActive, t.CreatedAt);
}

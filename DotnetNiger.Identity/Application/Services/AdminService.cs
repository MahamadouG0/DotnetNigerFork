using Microsoft.EntityFrameworkCore;
using DotnetNiger.Identity.Infrastructure;
using DotnetNiger.Identity.Application.DTOs;

namespace DotnetNiger.Identity.Application.Services;

public class AdminService
{
    private readonly IdentityDbContext _db;

    public AdminService(IdentityDbContext db) => _db = db;

    public async Task<object> GetSystemStatsAsync()
    {
        var totalTenants = await _db.Tenants.CountAsync();
        var totalUsers = await _db.Users.IgnoreQueryFilters().CountAsync();
        var totalRoles = await _db.Roles.IgnoreQueryFilters().CountAsync();
        var totalPermissions = await _db.Permissions.IgnoreQueryFilters().CountAsync();

        return new
        {
            totalTenants,
            totalUsers,
            totalRoles,
            totalPermissions,
            activeTenants = await _db.Tenants.CountAsync(t => t.IsActive)
        };
    }

    public async Task<List<UserResponse>> GetAllUsersAcrossTenantsAsync()
    {
        var users = await _db.Users.IgnoreQueryFilters().ToListAsync();
        return users.Select(u => new UserResponse(
            u.Id, u.Email!, u.FirstName, u.LastName, u.AvatarUrl,
            u.TenantId, u.IsActive, u.EmailConfirmed, u.CreatedAt,
            new List<string>())).ToList();
    }
}

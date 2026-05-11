using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using DotnetNiger.Identity.Application.DTOs;

namespace DotnetNiger.Identity.Application.Services;

public class RoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IdentityDbContext _db;

    public RoleService(RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager, IdentityDbContext db)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _db = db;
    }

    public async Task<RoleResponse> CreateAsync(CreateRoleRequest request)
    {
        var role = new ApplicationRole
        {
            Name = request.Name, Description = request.Description, TenantId = request.TenantId
        };
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        return new RoleResponse(role.Id, role.Name!, role.Description, role.TenantId, 0);
    }

    public async Task<List<RoleResponse>> GetByTenantAsync(Guid tenantId)
    {
        var roles = await _db.Roles.Where(r => r.TenantId == tenantId).ToListAsync();
        var result = new List<RoleResponse>();
        foreach (var role in roles)
        {
            var count = await _db.UserRoles.CountAsync(ur => ur.RoleId == role.Id);
            result.Add(new RoleResponse(role.Id, role.Name!, role.Description, role.TenantId, count));
        }
        return result;
    }

    public async Task<RoleResponse> UpdateAsync(Guid id, UpdateRoleRequest request)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) throw new KeyNotFoundException("Rôle non trouvé");

        role.Description = request.Description ?? role.Description;
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        var count = await _db.UserRoles.CountAsync(ur => ur.RoleId == role.Id);
        return new RoleResponse(role.Id, role.Name!, role.Description, role.TenantId, count);
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role != null) await _roleManager.DeleteAsync(role);
    }

    public async Task AssignToUserAsync(Guid userId, Guid roleId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (user == null || role == null) throw new KeyNotFoundException();

        await _userManager.AddToRoleAsync(user, role.Name!);
    }

    public async Task RemoveFromUserAsync(Guid userId, Guid roleId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (user == null || role == null) throw new KeyNotFoundException();

        await _userManager.RemoveFromRoleAsync(user, role.Name!);
    }

    public async Task<List<RoleResponse>> GetUserRolesAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) throw new KeyNotFoundException();

        var roleNames = await _userManager.GetRolesAsync(user);
        var roles = await _db.Roles
            .Where(r => roleNames.Contains(r.Name!))
            .ToListAsync();

        return roles.Select(r => new RoleResponse(
            r.Id, r.Name!, r.Description, r.TenantId, 0)).ToList();
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using DotnetNiger.Identity.Application.DTOs;

namespace DotnetNiger.Identity.Application.Services;

public class AdminService
{
    private readonly IdentityDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender<ApplicationUser> _emailSender;
    private readonly SmtpOptions _smtp;

    public AdminService(IdentityDbContext db, UserManager<ApplicationUser> userManager,
        IEmailSender<ApplicationUser> emailSender, Microsoft.Extensions.Options.IOptions<SmtpOptions> smtp)
    {
        _db = db;
        _userManager = userManager;
        _emailSender = emailSender;
        _smtp = smtp.Value;
    }

    public async Task<object> GetSystemStatsAsync()
    {
        var totalTenants = await _db.Tenants.CountAsync();
        var totalUsers = await _db.Users.IgnoreQueryFilters().CountAsync();
        var totalRoles = await _db.Roles.IgnoreQueryFilters().CountAsync();
        var totalPermissions = await _db.Permissions.IgnoreQueryFilters().CountAsync();
        var totalApiKeys = await _db.TenantApiKeys.IgnoreQueryFilters().CountAsync();
        var totalServices = await _db.ExternalServices.IgnoreQueryFilters().CountAsync();
        var totalClients = await _db.TenantClients.IgnoreQueryFilters().CountAsync();

        return new
        {
            totalTenants,
            totalUsers,
            totalRoles,
            totalPermissions,
            totalApiKeys,
            totalServices,
            totalClients,
            activeTenants = await _db.Tenants.CountAsync(t => t.IsActive)
        };
    }

    public async Task InviteAsync(string email, string role)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
            throw new InvalidOperationException("Un utilisateur avec cet email existe déjà");

        var tenant = await _db.Tenants.FirstOrDefaultAsync();
        if (tenant == null)
            throw new InvalidOperationException("Aucun tenant trouvé");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            TenantId = tenant.Id,
            IsActive = true,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, GenerateTemporaryPassword());
        if (!result.Succeeded)
            throw new InvalidOperationException($"Erreur: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await _userManager.AddToRoleAsync(user, role);

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var inviteUrl = $"{_smtp.AppBaseUrl.TrimEnd('/')}/Account/Register?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        if (_emailSender is EmailSender typed)
            await typed.SendInviteEmailAsync(email, inviteUrl, role);
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%";
        var data = new byte[16];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(data);
        return new string(data.Select(b => chars[b % chars.Length]).ToArray()) + "Aa1!";
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

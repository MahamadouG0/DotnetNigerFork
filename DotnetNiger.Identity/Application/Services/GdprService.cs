using System.IO.Compression;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using DotnetNiger.Identity.Application.DTOs;

namespace DotnetNiger.Identity.Application.Services;

public class GdprService
{
    private readonly IdentityDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public GdprService(IdentityDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task RecordConsentAsync(Guid userId, string consentType, string consentVersion, bool granted, string? ipAddress, string? userAgent)
    {
        var consent = new UserConsent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ConsentType = consentType,
            ConsentVersion = consentVersion,
            Granted = granted,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
        _db.UserConsents.Add(consent);
        await _db.SaveChangesAsync();
    }

    public async Task<List<ConsentResponse>> GetConsentHistoryAsync(Guid userId)
    {
        return await _db.UserConsents
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ConsentResponse(c.ConsentType, c.ConsentVersion, c.Granted, c.CreatedAt))
            .ToListAsync();
    }

    public async Task<List<ConsentResponse>> GetLatestConsentsAsync(Guid userId)
    {
        var all = await _db.UserConsents
            .Where(c => c.UserId == userId)
            .GroupBy(c => c.ConsentType)
            .Select(g => g.OrderByDescending(c => c.CreatedAt).First())
            .ToListAsync();

        return all.Select(c => new ConsentResponse(c.ConsentType, c.ConsentVersion, c.Granted, c.CreatedAt)).ToList();
    }

    public async Task<byte[]> ExportUserDataAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) throw new KeyNotFoundException("User not found");

        var roles = await _userManager.GetRolesAsync(user);
        var consents = await _db.UserConsents.Where(c => c.UserId == userId).ToListAsync();
        var auditLogs = await _db.AuditLogs.Where(a => a.UserId == userId).OrderByDescending(a => a.CreatedAt).Take(500).ToListAsync();
        var tenants = await _db.Tenants.Where(t => t.Id == user.TenantId).ToListAsync();

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var profileEntry = archive.CreateEntry("profile.json");
            using (var writer = new StreamWriter(profileEntry.Open()))
            {
                var profile = new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.AvatarUrl,
                    user.TenantId,
                    user.IsActive,
                    user.EmailConfirmed,
                    user.TwoFactorEnabled,
                    user.CreatedAt
                };
                await writer.WriteAsync(JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }));
            }

            var rolesEntry = archive.CreateEntry("roles.json");
            using (var writer = new StreamWriter(rolesEntry.Open()))
            {
                await writer.WriteAsync(JsonSerializer.Serialize(roles, new JsonSerializerOptions { WriteIndented = true }));
            }

            if (consents.Count != 0)
            {
                var consentsEntry = archive.CreateEntry("consents.json");
                using (var writer = new StreamWriter(consentsEntry.Open()))
                {
                    await writer.WriteAsync(JsonSerializer.Serialize(consents, new JsonSerializerOptions { WriteIndented = true }));
                }
            }

            if (auditLogs.Count != 0)
            {
                var logsEntry = archive.CreateEntry("audit-logs.json");
                using (var writer = new StreamWriter(logsEntry.Open()))
                {
                    await writer.WriteAsync(JsonSerializer.Serialize(auditLogs, new JsonSerializerOptions { WriteIndented = true }));
                }
            }

            if (tenants.Count != 0)
            {
                var tenantsEntry = archive.CreateEntry("tenants.json");
                using (var writer = new StreamWriter(tenantsEntry.Open()))
                {
                    await writer.WriteAsync(JsonSerializer.Serialize(tenants, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
        }

        return memoryStream.ToArray();
    }

    public async Task ForgetMeAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) throw new KeyNotFoundException("User not found");

        var anonymizedEmail = $"anonymized-{userId:N}@dotnetniger.com";
        user.Email = anonymizedEmail;
        user.UserName = anonymizedEmail;
        user.FirstName = "Anonymized";
        user.LastName = "User";
        user.AvatarUrl = null;
        user.IsActive = false;
        user.EmailConfirmed = false;
        user.EmailConfirmationCode = null;
        user.EmailConfirmationCodeExpiry = null;
        user.PhoneNumber = null;
        user.TwoFactorEnabled = false;
        user.SecurityStamp = Guid.NewGuid().ToString();

        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count != 0)
            await _userManager.RemoveFromRolesAsync(user, roles);

        var logins = await _userManager.GetLoginsAsync(user);
        foreach (var login in logins)
            await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);

        var auditLogs = await _db.AuditLogs.Where(a => a.UserId == userId).ToListAsync();
        foreach (var log in auditLogs)
        {
            log.Description = "[anonymized]";
            log.IpAddress = null;
        }

        var oldConsents = await _db.UserConsents
            .Where(c => c.UserId == userId && c.CreatedAt < DateTime.UtcNow.AddDays(-30))
            .ToListAsync();
        _db.UserConsents.RemoveRange(oldConsents);

        await _db.SaveChangesAsync();
    }
}

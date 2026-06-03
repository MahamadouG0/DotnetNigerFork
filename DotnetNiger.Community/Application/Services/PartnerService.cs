using DotnetNiger.Community.Domain;
using DotnetNiger.Community.Domain.Entities;
using DotnetNiger.Community.Infrastructure;
using DotnetNiger.Community.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Community.Application.Services;

public class PartnerService(AppDbContext db) : IPartnerService
{
    public async Task<List<PartnerResponse>> GetAllActiveAsync(string? partnerType)
    {
        var q = db.Set<Partner>().Where(p => p.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(partnerType))
            q = q.Where(p => p.PartnerType == partnerType);

        return await q
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Select(p => MapPartner(p))
            .ToListAsync();
    }

    public async Task<PartnerResponse?> GetByIdAsync(Guid id)
    {
        var p = await db.Set<Partner>().FindAsync(id);
        return p is null ? null : MapPartner(p);
    }

    public async Task<PartnerResponse> CreateAsync(CreatePartnerRequest request)
    {
        var partner = new Partner
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = GenerateSlug(request.Name),
            Description = request.Description,
            LogoUrl = request.LogoUrl,
            WebsiteUrl = request.WebsiteUrl,
            PartnerType = request.PartnerType,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };

        db.Add(partner);
        await db.SaveChangesAsync();
        return MapPartner(partner);
    }

    public async Task<PartnerResponse?> UpdateAsync(Guid id, UpdatePartnerRequest request)
    {
        var p = await db.Set<Partner>().FindAsync(id);
        if (p is null) return null;

        p.Name = request.Name;
        p.Slug = GenerateSlug(request.Name);
        p.Description = request.Description;
        p.LogoUrl = request.LogoUrl;
        p.WebsiteUrl = request.WebsiteUrl;
        p.PartnerType = request.PartnerType;
        p.SortOrder = request.SortOrder;
        p.IsActive = request.IsActive;
        p.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return MapPartner(p);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var p = await db.Set<Partner>().FindAsync(id);
        if (p is null) return false;
        db.Remove(p);
        await db.SaveChangesAsync();
        return true;
    }

    private static PartnerResponse MapPartner(Partner p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Slug = p.Slug,
        Description = p.Description,
        LogoUrl = p.LogoUrl,
        WebsiteUrl = p.WebsiteUrl,
        PartnerType = p.PartnerType,
        SortOrder = p.SortOrder,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt
    };

    private static string GenerateSlug(string text) => SlugGenerator.Generate(text);
}

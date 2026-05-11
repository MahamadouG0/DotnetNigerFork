using DotnetNiger.Community.Infrastructure;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Community.Application.Services;

public class ResourceService(AppDbContext db) : IResourceService
{
    public async Task<PaginatedResponse<ResourceResponse>> GetAllAsync(string? resourceType, string? level, string? query, int page = 1, int pageSize = 10)
    {
        var q = db.Resources.AsQueryable();

        if (!string.IsNullOrWhiteSpace(resourceType)) q = q.Where(r => r.ResourceType == resourceType);
        if (!string.IsNullOrWhiteSpace(level)) q = q.Where(r => r.Level == level);
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(r => r.Title.Contains(query) || r.Description.Contains(query));

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => MapResource(r))
            .ToListAsync();

        return new PaginatedResponse<ResourceResponse> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<ResourceResponse?> GetByIdAsync(Guid id)
    {
        var r = await db.Resources.FindAsync(id);
        return r is null ? null : MapResource(r);
    }

    public async Task<ResourceResponse> CreateAsync(CreateResourceRequest request)
    {
        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = GenerateSlug(request.Title),
            Description = request.Description,
            Url = request.Url,
            ResourceType = request.ResourceType,
            Level = request.Level,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Resources.Add(resource);
        await db.SaveChangesAsync();
        return MapResource(resource);
    }

    public async Task<ResourceResponse?> UpdateAsync(Guid id, CreateResourceRequest request)
    {
        var r = await db.Resources.FindAsync(id);
        if (r is null) return null;

        r.Title = request.Title;
        r.Slug = GenerateSlug(request.Title);
        r.Description = request.Description;
        r.Url = request.Url;
        r.ResourceType = request.ResourceType;
        r.Level = request.Level;
        r.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return MapResource(r);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var r = await db.Resources.FindAsync(id);
        if (r is null) return false;
        db.Resources.Remove(r);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<ResourceResponse?> IncrementViewCountAsync(Guid id)
    {
        var r = await db.Resources.FindAsync(id);
        if (r is null) return null;
        r.ViewCount++;
        await db.SaveChangesAsync();
        return MapResource(r);
    }

    private static ResourceResponse MapResource(Resource r) => new()
    {
        Id = r.Id,
        Title = r.Title,
        Slug = r.Slug,
        Description = r.Description,
        Url = r.Url,
        ResourceType = r.ResourceType,
        Level = r.Level,
        ViewCount = r.ViewCount,
        CreatedAt = r.CreatedAt
    };

    private static string GenerateSlug(string text)
    {
        return text.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "").Replace(".", "").Replace(",", "")
            .Replace("é", "e").Replace("è", "e").Replace("ê", "e")
            .Replace("à", "a").Replace("â", "a")
            .Replace("ù", "u").Replace("û", "u")
            .Replace("ô", "o").Replace("ö", "o")
            .Replace("î", "i").Replace("ï", "i")
            .Replace("ç", "c")
            .Replace("\"", "").Replace("'", "");
    }
}

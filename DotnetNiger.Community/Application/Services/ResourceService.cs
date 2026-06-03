using DotnetNiger.Community.Infrastructure;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Domain;
using DotnetNiger.Community.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Community.Application.Services;

public class ResourceService(AppDbContext db) : IResourceService
{
    public async Task<PaginatedResponse<ResourceResponse>> GetAllAsync(string? resourceType, string? level, string? query, string? tag, Guid? categoryId, int page = 1, int pageSize = 10)
    {
        var q = db.Resources
            .Include(r => r.ResourceCategories)
            .Include(r => r.ResourceTags).ThenInclude(rt => rt.Tag)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(resourceType)) q = q.Where(r => r.ResourceType == resourceType);
        if (!string.IsNullOrWhiteSpace(level)) q = q.Where(r => r.Level == level);
        if (!string.IsNullOrWhiteSpace(tag))
            q = q.Where(r => r.ResourceTags.Any(rt => rt.Tag.Slug == tag));
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(r => r.Title.Contains(query) || r.Description.Contains(query));
        if (categoryId.HasValue)
            q = q.Where(r => r.ResourceCategories.Any(rc => rc.CategoryId == categoryId.Value));

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
        var r = await db.Resources
            .Include(r => r.ResourceCategories)
            .Include(r => r.ResourceTags).ThenInclude(rt => rt.Tag)
            .FirstOrDefaultAsync(r => r.Id == id);
        return r is null ? null : MapResource(r);
    }

    public async Task<ResourceResponse> CreateAsync(CreateResourceRequest request, Guid userId)
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
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Resources.Add(resource);

        if (request.CategoryIds?.Count > 0)
        {
            foreach (var catId in request.CategoryIds)
            {
                db.Set<ResourceCategory>().Add(new ResourceCategory { ResourceId = resource.Id, CategoryId = catId });
            }
        }

        await AssignTags(resource, request.TagNames);
        await db.SaveChangesAsync();
        return MapResource(resource);
    }

    public async Task<ResourceResponse?> UpdateAsync(Guid id, CreateResourceRequest request, Guid userId, bool isAdmin)
    {
        var r = await db.Resources
            .Include(r => r.ResourceTags)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (r is null) return null;
        if (r.CreatedBy != userId && !isAdmin)
            throw new UnauthorizedAccessException("Vous n'êtes pas autorisé à modifier cette ressource.");

        r.Title = request.Title;
        r.Slug = GenerateSlug(request.Title);
        r.Description = request.Description;
        r.Url = request.Url;
        r.ResourceType = request.ResourceType;
        r.Level = request.Level;
        r.UpdatedAt = DateTime.UtcNow;

        var existingCategories = await db.Set<ResourceCategory>().Where(rc => rc.ResourceId == id).ToListAsync();
        db.Set<ResourceCategory>().RemoveRange(existingCategories);

        if (request.CategoryIds?.Count > 0)
        {
            foreach (var catId in request.CategoryIds)
            {
                db.Set<ResourceCategory>().Add(new ResourceCategory { ResourceId = id, CategoryId = catId });
            }
        }

        db.ResourceTags.RemoveRange(r.ResourceTags);
        await AssignTags(r, request.TagNames);
        await db.SaveChangesAsync();
        return MapResource(r);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, bool isAdmin)
    {
        var r = await db.Resources.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id);
        if (r is null) return false;
        if (r.CreatedBy != userId && !isAdmin)
            throw new UnauthorizedAccessException("Vous n'êtes pas autorisé à supprimer cette ressource.");
        r.IsDeleted = true;
        r.UpdatedAt = DateTime.UtcNow;
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

    private async Task AssignTags(Resource resource, List<string> tagNames)
    {
        foreach (var name in tagNames.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            var slug = GenerateSlug(name);
            var tag = await db.Tags.FirstOrDefaultAsync(t => t.Slug == slug);
            if (tag is null)
            {
                tag = new Tag { Id = Guid.NewGuid(), Name = name, Slug = slug };
                db.Tags.Add(tag);
            }
            resource.ResourceTags.Add(new ResourceTag { ResourceId = resource.Id, TagId = tag.Id });
        }
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
        CreatedBy = r.CreatedBy,
        ViewCount = r.ViewCount,
        CreatedAt = r.CreatedAt,
        CategoryIds = r.ResourceCategories.Select(rc => rc.CategoryId).ToList(),
        Tags = r.ResourceTags.Select(rt => new TagResponse
        {
            Id = rt.Tag.Id,
            Name = rt.Tag.Name,
            Slug = rt.Tag.Slug,
            UsageCount = rt.Tag.UsageCount
        }).ToList()
    };

    private static string GenerateSlug(string text) => SlugGenerator.Generate(text);
}

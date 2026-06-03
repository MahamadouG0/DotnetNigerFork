using DotnetNiger.Community.Infrastructure;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Notifications;
using DotnetNiger.Community.Domain;
using DotnetNiger.Community.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Community.Application.Services;

public class ProjectService(AppDbContext db, INotificationService notificationService) : IProjectService
{
    public async Task<PaginatedResponse<ProjectResponse>> GetAllAsync(string? status, string? query, int page = 1, int pageSize = 10)
    {
        var q = db.Set<Project>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(p => p.Title.Contains(query) || p.Description.Contains(query) || p.Technologies.Contains(query));

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapProject(p))
            .ToListAsync();

        return new PaginatedResponse<ProjectResponse> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<List<ProjectResponse>> GetFeaturedAsync()
    {
        return await db.Set<Project>()
            .Where(p => p.IsFeatured && p.IsPublished)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => MapProject(p))
            .ToListAsync();
    }

    public async Task<ProjectResponse?> GetByIdAsync(Guid id)
    {
        var p = await db.Set<Project>().FindAsync(id);
        return p is null ? null : MapProject(p);
    }

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, Guid userId, string authorName)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = GenerateSlug(request.Title),
            Description = request.Description,
            Url = request.Url,
            GithubUrl = request.GithubUrl,
            ImageUrl = request.ImageUrl,
            Technologies = request.Technologies,
            Status = request.Status,
            CreatedBy = userId,
            AuthorName = authorName,
            IsFeatured = request.IsFeatured,
            IsPublished = request.IsPublished
        };

        db.Add(project);
        await db.SaveChangesAsync();
        _ = notificationService.NotifyNewProjectAsync(project.Title, project.Description, project.AuthorName);
        return MapProject(project);
    }

    public async Task<ProjectResponse?> UpdateAsync(Guid id, UpdateProjectRequest request, Guid userId, bool isAdmin)
    {
        var p = await db.Set<Project>().FindAsync(id);
        if (p is null) return null;
        if (p.CreatedBy != userId && !isAdmin)
            throw new UnauthorizedAccessException("Vous n'êtes pas autorisé à modifier ce projet.");

        p.Title = request.Title;
        p.Slug = GenerateSlug(request.Title);
        p.Description = request.Description;
        p.Url = request.Url;
        p.GithubUrl = request.GithubUrl;
        p.ImageUrl = request.ImageUrl;
        p.Technologies = request.Technologies;
        p.Status = request.Status;
        p.IsFeatured = request.IsFeatured;
        p.IsPublished = request.IsPublished;
        p.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return MapProject(p);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, bool isAdmin)
    {
        var p = await db.Set<Project>().IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
        if (p is null) return false;
        if (p.CreatedBy != userId && !isAdmin)
            throw new UnauthorizedAccessException("Vous n'êtes pas autorisé à supprimer ce projet.");
        p.IsDeleted = true;
        p.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    private static ProjectResponse MapProject(Project p) => new()
    {
        Id = p.Id,
        Title = p.Title,
        Slug = p.Slug,
        Description = p.Description,
        Url = p.Url,
        GithubUrl = p.GithubUrl,
        ImageUrl = p.ImageUrl,
        Technologies = p.Technologies,
        Status = p.Status,
        CreatedBy = p.CreatedBy,
        AuthorName = p.AuthorName,
        IsFeatured = p.IsFeatured,
        IsPublished = p.IsPublished,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };

    private static string GenerateSlug(string text) => SlugGenerator.Generate(text);
}

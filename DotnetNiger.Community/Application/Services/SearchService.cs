using DotnetNiger.Community.Infrastructure;
using DotnetNiger.Community.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Community.Application.Services;

public class SearchService(AppDbContext db) : ISearchService
{
    public async Task<PaginatedResponse<SearchResultResponse>> SearchAsync(SearchQueryRequest request)
    {
        var results = new List<SearchResultResponse>();

        if (string.IsNullOrWhiteSpace(request.Type) || request.Type == "Post")
        {
            var q = db.Posts.Where(p => p.IsPublished);
            if (!string.IsNullOrWhiteSpace(request.Query))
                q = q.Where(p => p.Title.Contains(request.Query) || p.Content.Contains(request.Query));

            var posts = await q
                .OrderByDescending(p => p.PublishedAt)
                .Select(p => new SearchResultResponse
                {
                    Type = "Post",
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    Excerpt = p.Excerpt,
                    Content = p.Content,
                    CoverImageUrl = p.CoverImageUrl,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();
            results.AddRange(posts);
        }

        if (string.IsNullOrWhiteSpace(request.Type) || request.Type == "Event")
        {
            var q = db.Events.Where(e => e.IsPublished);
            if (!string.IsNullOrWhiteSpace(request.Query))
                q = q.Where(e => e.Title.Contains(request.Query) || e.Description.Contains(request.Query));

            var events = await q
                .OrderByDescending(e => e.StartDate)
                .Select(e => new SearchResultResponse
                {
                    Type = "Event",
                    Id = e.Id,
                    Title = e.Title,
                    Slug = e.Slug,
                    Description = e.Description,
                    CoverImageUrl = e.CoverImageUrl,
                    StartDateTime = e.StartDate,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();
            results.AddRange(events);
        }

        if (string.IsNullOrWhiteSpace(request.Type) || request.Type == "Resource")
        {
            var q = db.Resources.AsQueryable();
            if (!string.IsNullOrWhiteSpace(request.Query))
                q = q.Where(r => r.Title.Contains(request.Query) || r.Description.Contains(request.Query));

            var resources = await q
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new SearchResultResponse
                {
                    Type = "Resource",
                    Id = r.Id,
                    Title = r.Title,
                    Slug = r.Slug,
                    Description = r.Description,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
            results.AddRange(resources);
        }

        var total = results.Count;
        var items = results
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PaginatedResponse<SearchResultResponse>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

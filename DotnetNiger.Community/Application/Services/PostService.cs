using DotnetNiger.Community.Infrastructure;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Community.Application.Services;

public class PostService(AppDbContext db) : IPostService
{
    public async Task<PaginatedResponse<PostResponse>> GetAllAsync(string? published, string? category, string? tag, string? query, int page = 1, int pageSize = 10)
    {
        var q = db.Posts
            .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .AsSplitQuery()
            .AsQueryable();

        if (published == "true") q = q.Where(p => p.IsPublished);
        if (published == "false") q = q.Where(p => !p.IsPublished);
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(p => p.PostCategories.Any(pc => pc.Category.Slug == category));
        if (!string.IsNullOrWhiteSpace(tag))
            q = q.Where(p => p.PostTags.Any(pt => pt.Tag.Slug == tag));
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(p => p.Title.Contains(query) || p.Content.Contains(query));

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapPost(p))
            .ToListAsync();

        return new PaginatedResponse<PostResponse> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<PostResponse?> GetByIdAsync(Guid id)
    {
        var post = await db.Posts
            .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);
        return post is null ? null : MapPost(post);
    }

    public async Task<PostResponse> CreateAsync(CreatePostRequest request, Guid authorId, string authorName)
    {
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = GenerateSlug(request.Title),
            Content = request.Content,
            Excerpt = request.Excerpt,
            CoverImageUrl = request.CoverImageUrl,
            AuthorId = authorId,
            AuthorName = authorName,
            PostType = request.PostType,
            IsPublished = request.IsPublished,
            PublishedAt = request.IsPublished ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await AssignCategories(post, request.CategoryIds);
        await AssignTags(post, request.TagNames);
        db.Posts.Add(post);
        await db.SaveChangesAsync();
        return (await GetByIdAsync(post.Id))!;
    }

    public async Task<PostResponse?> UpdateAsync(Guid id, UpdatePostRequest request)
    {
        var post = await db.Posts
            .Include(p => p.PostCategories)
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (post is null) return null;

        post.Title = request.Title;
        post.Slug = GenerateSlug(request.Title);
        post.Content = request.Content;
        post.Excerpt = request.Excerpt;
        post.CoverImageUrl = request.CoverImageUrl;
        post.PostType = request.PostType;
        post.IsPublished = request.IsPublished;
        if (request.IsPublished && post.PublishedAt is null)
            post.PublishedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;

        db.PostCategories.RemoveRange(post.PostCategories);
        db.PostTags.RemoveRange(post.PostTags);
        await AssignCategories(post, request.CategoryIds);
        await AssignTags(post, request.TagNames);
        await db.SaveChangesAsync();
        return (await GetByIdAsync(post.Id))!;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var post = await db.Posts.FindAsync(id);
        if (post is null) return false;
        db.Posts.Remove(post);
        await db.SaveChangesAsync();
        return true;
    }

    private static PostResponse MapPost(Post p) => new()
    {
        Id = p.Id,
        Title = p.Title,
        Slug = p.Slug,
        Content = p.Content,
        Excerpt = p.Excerpt,
        CoverImageUrl = p.CoverImageUrl,
        AuthorId = p.AuthorId,
        AuthorName = p.AuthorName,
        AuthorAvatar = p.AuthorAvatar,
        PostType = p.PostType,
        IsPublished = p.IsPublished,
        PublishedAt = p.PublishedAt ?? DateTime.MinValue,
        ViewCount = p.ViewCount,
        CreatedAt = p.CreatedAt,
        Categories = p.PostCategories.Select(pc => new CategoryResponse
        {
            Id = pc.Category.Id,
            Name = pc.Category.Name,
            Slug = pc.Category.Slug,
            Description = pc.Category.Description,
            PostCount = pc.Category.PostCount
        }).ToList(),
        Tags = p.PostTags.Select(pt => new TagResponse
        {
            Id = pt.Tag.Id,
            Name = pt.Tag.Name,
            Slug = pt.Tag.Slug,
            PostCount = pt.Tag.PostCount
        }).ToList()
    };

    private async Task AssignCategories(Post post, List<Guid> categoryIds)
    {
        var categories = await db.Categories.Where(c => categoryIds.Contains(c.Id)).ToListAsync();
        foreach (var cat in categories)
            post.PostCategories.Add(new PostCategory { PostId = post.Id, CategoryId = cat.Id });
    }

    private async Task AssignTags(Post post, List<string> tagNames)
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
            post.PostTags.Add(new PostTag { PostId = post.Id, TagId = tag.Id });
        }
    }

    private static string GenerateSlug(string text)
    {
        return text.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("é", "e").Replace("è", "e").Replace("ê", "e")
            .Replace("à", "a").Replace("â", "a")
            .Replace("ù", "u").Replace("û", "u")
            .Replace("ô", "o").Replace("ö", "o")
            .Replace("î", "i").Replace("ï", "i")
            .Replace("ç", "c")
            .Replace("\"", "").Replace("'", "");
    }
}

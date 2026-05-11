using DotnetNiger.Community.Infrastructure;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Community.Application.Services;

public class CommentService(AppDbContext db) : ICommentService
{
    public async Task<List<CommentResponse>> GetByPostIdAsync(Guid postId)
    {
        var comments = await db.Comments
            .Where(c => c.PostId == postId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return BuildTree(comments);
    }

    public async Task<List<CommentResponse>> GetByEventIdAsync(Guid eventId)
    {
        var comments = await db.Comments
            .Where(c => c.EventId == eventId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return BuildTree(comments);
    }

    public async Task<CommentResponse?> GetByIdAsync(Guid id)
    {
        var comment = await db.Comments.FindAsync(id);
        return comment is null ? null : MapComment(comment);
    }

    public async Task<CommentResponse> CreateAsync(CreateCommentRequest request, Guid userId, string authorName, string authorAvatar)
    {
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Content = request.Content,
            UserId = userId,
            AuthorName = authorName,
            AuthorAvatar = authorAvatar,
            PostId = request.PostId,
            EventId = request.EventId,
            ParentCommentId = request.ParentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        return MapComment(comment);
    }

    public async Task<CommentResponse?> UpdateAsync(Guid id, UpdateCommentRequest request, Guid userId)
    {
        var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (comment is null) return null;

        comment.Content = request.Content;
        comment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return MapComment(comment);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, bool deleteAllReplies = false)
    {
        var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (comment is null) return false;

        if (deleteAllReplies)
        {
            var replies = await db.Comments.Where(c => c.ParentCommentId == id).ToListAsync();
            db.Comments.RemoveRange(replies);
        }
        else
        {
            var hasReplies = await db.Comments.AnyAsync(c => c.ParentCommentId == id);
            if (hasReplies)
            {
                comment.Content = "[Supprimé]";
                comment.UserId = Guid.Empty;
                await db.SaveChangesAsync();
                return true;
            }
        }

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
        return true;
    }

    private static List<CommentResponse> BuildTree(List<Comment> comments)
    {
        var map = comments.Select(MapComment).ToDictionary(c => c.Id);
        var roots = new List<CommentResponse>();

        foreach (var c in map.Values)
        {
            if (c.ParentCommentId is null || !map.ContainsKey(c.ParentCommentId.Value))
                roots.Add(c);
            else if (map.TryGetValue(c.ParentCommentId.Value, out var parent))
                parent.Replies.Add(c);
        }

        return roots;
    }

    private static CommentResponse MapComment(Comment c) => new()
    {
        Id = c.Id,
        Content = c.Content,
        UserId = c.UserId,
        AuthorName = c.AuthorName,
        AuthorAvatar = c.AuthorAvatar,
        PostId = c.PostId ?? Guid.Empty,
        EventId = c.EventId ?? Guid.Empty,
        ParentCommentId = c.ParentCommentId,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}

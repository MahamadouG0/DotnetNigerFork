using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Domain.Entities;
using DotnetNiger.Community.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotnetNiger.Community.Tests.Application.Services;

public class CommentServiceTests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    [Fact]
    public async Task GetByPostIdAsync_ReturnsOrderedComments()
    {
        var db = CreateDb();
        var postId = Guid.NewGuid();
        db.Comments.AddRange(
            new Comment { Id = Guid.NewGuid(), Content = "C1", PostId = postId, CreatedAt = DateTime.UtcNow.AddHours(-1) },
            new Comment { Id = Guid.NewGuid(), Content = "C2", PostId = postId, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var svc = new CommentService(db);
        var result = await svc.GetByPostIdAsync(postId);

        result.Should().HaveCount(2);
        result[0].Content.Should().Be("C2");
    }

    [Fact]
    public async Task CreateAsync_AddsComment()
    {
        var db = CreateDb();
        var request = new CreateCommentRequest { Content = "New comment", PostId = Guid.NewGuid() };

        var svc = new CommentService(db);
        var result = await svc.CreateAsync(request, Guid.NewGuid(), "Author", "");

        result.Content.Should().Be("New comment");
        db.Comments.Count().Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_RemovesComment()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();
        var comment = new Comment { Id = Guid.NewGuid(), Content = "Test", UserId = userId, PostId = Guid.NewGuid() };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        var svc = new CommentService(db);
        var deleted = await svc.DeleteAsync(comment.Id, userId);

        deleted.Should().BeTrue();
        db.Comments.Count().Should().Be(0);
    }
}

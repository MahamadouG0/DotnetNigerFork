using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Domain.Entities;
using DotnetNiger.Community.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotnetNiger.Community.Tests.Application.Services;

public class PostServiceTests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedPosts()
    {
        var db = CreateDb();
        db.Posts.AddRange(
            new Post { Id = Guid.NewGuid(), Title = "P1", Slug = "p1", Content = "C1" },
            new Post { Id = Guid.NewGuid(), Title = "P2", Slug = "p2", Content = "C2" });
        await db.SaveChangesAsync();

        var svc = new PostService(db);
        var result = await svc.GetAllAsync(null, null, null, null, 1, 10);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var db = CreateDb();
        var svc = new PostService(db);
        var result = await svc.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_AddsPost()
    {
        var db = CreateDb();
        var request = new CreatePostRequest { Title = "New Post", Content = "Content", PostType = "article" };

        var svc = new PostService(db);
        var result = await svc.CreateAsync(request, Guid.NewGuid(), "Author");

        result.Title.Should().Be("New Post");
        db.Posts.Count().Should().Be(1);
    }
}

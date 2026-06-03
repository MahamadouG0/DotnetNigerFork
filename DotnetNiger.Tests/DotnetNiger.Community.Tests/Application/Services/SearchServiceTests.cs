using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Domain.Entities;
using DotnetNiger.Community.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotnetNiger.Community.Tests.Application.Services;

public class SearchServiceTests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    [Fact]
    public async Task SearchAsync_ReturnsMatchingPosts()
    {
        var db = CreateDb();
        db.Posts.AddRange(
            new Post { Id = Guid.NewGuid(), Title = "Alpha Post", Slug = "alpha", Content = "Content", IsPublished = true },
            new Post { Id = Guid.NewGuid(), Title = "Beta Post", Slug = "beta", Content = "Other", IsPublished = true });
        await db.SaveChangesAsync();

        var svc = new SearchService(db);
        var result = await svc.SearchAsync(new SearchQueryRequest { Query = "Alpha", Type = "Post", Page = 1, PageSize = 10 });

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Alpha Post");
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenNoMatch()
    {
        var db = CreateDb();
        var svc = new SearchService(db);
        var result = await svc.SearchAsync(new SearchQueryRequest { Query = "zzz", Type = "Post", Page = 1, PageSize = 10 });

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
}

using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Domain.Entities;
using DotnetNiger.Community.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Application.Services;

public class AdminServiceTests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsCounts()
    {
        var db = CreateDb();
        db.Posts.Add(new Post { Id = Guid.NewGuid(), Title = "P1", Slug = "p1", IsPublished = true });
        db.Posts.Add(new Post { Id = Guid.NewGuid(), Title = "P2", Slug = "p2" });
        db.Events.Add(new Event { Id = Guid.NewGuid(), Title = "E1", Slug = "e1" });
        db.Resources.Add(new Resource { Id = Guid.NewGuid(), Title = "R1", Slug = "r1" });
        db.Comments.Add(new Comment { Id = Guid.NewGuid(), Content = "C1", PostId = Guid.NewGuid() });
        db.Members.Add(new Member { Id = Guid.NewGuid(), FullName = "M1" });
        await db.SaveChangesAsync();

        var svc = new AdminService(db, Mock.Of<IIdentityApiClient>());
        var result = await svc.GetDashboardAsync();

        result.PostsCount.Should().Be(2);
        result.PublishedPostsCount.Should().Be(1);
        result.EventsCount.Should().Be(1);
        result.ResourcesCount.Should().Be(1);
        result.MembersCount.Should().Be(1);
        result.CommentsCount.Should().Be(1);
    }

    [Fact]
    public async Task GetDashboardAsync_WithZeroData_ReturnsZero()
    {
        var db = CreateDb();
        var svc = new AdminService(db, Mock.Of<IIdentityApiClient>());
        var result = await svc.GetDashboardAsync();

        result.PostsCount.Should().Be(0);
        result.EventsCount.Should().Be(0);
    }
}

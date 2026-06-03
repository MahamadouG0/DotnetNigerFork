using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Domain.Entities;
using DotnetNiger.Community.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotnetNiger.Community.Tests.Application.Services;

public class ResourceServiceTests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResources()
    {
        var db = CreateDb();
        db.Resources.AddRange(
            new Resource { Id = Guid.NewGuid(), Title = "R1", Slug = "r1", ResourceType = "article" },
            new Resource { Id = Guid.NewGuid(), Title = "R2", Slug = "r2", ResourceType = "video" });
        await db.SaveChangesAsync();

        var svc = new ResourceService(db);
        var result = await svc.GetAllAsync(null, null, null, null, null, 1, 10);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByType()
    {
        var db = CreateDb();
        db.Resources.AddRange(
            new Resource { Id = Guid.NewGuid(), Title = "R1", Slug = "r1", ResourceType = "article" },
            new Resource { Id = Guid.NewGuid(), Title = "R2", Slug = "r2", ResourceType = "video" });
        await db.SaveChangesAsync();

        var svc = new ResourceService(db);
        var result = await svc.GetAllAsync("article", null, null, null, null, 1, 10);

        result.Items.Should().HaveCount(1);
        result.Items[0].ResourceType.Should().Be("article");
    }
}

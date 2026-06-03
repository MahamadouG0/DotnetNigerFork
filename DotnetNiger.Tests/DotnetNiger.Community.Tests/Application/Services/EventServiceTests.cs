using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Notifications;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Domain.Entities;
using DotnetNiger.Community.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Application.Services;

public class EventServiceTests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    private static Mock<INotificationService> CreateNotificationMock()
    {
        return new Mock<INotificationService>();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedEvents()
    {
        var db = CreateDb();
        db.Events.AddRange(
            new Event { Id = Guid.NewGuid(), Title = "E1", Slug = "e1" },
            new Event { Id = Guid.NewGuid(), Title = "E2", Slug = "e2" });
        await db.SaveChangesAsync();

        var svc = new EventService(db, CreateNotificationMock().Object);
        var result = await svc.GetAllAsync(null, null, null, null, null, null, null, 1, 10);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var db = CreateDb();
        var svc = new EventService(db, CreateNotificationMock().Object);
        var result = await svc.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_AddsEvent()
    {
        var db = CreateDb();
        var request = new CreateEventRequest { Title = "New Event", Description = "Desc", StartDate = DateTime.UtcNow.AddDays(1), EndDate = DateTime.UtcNow.AddDays(2) };

        var svc = new EventService(db, CreateNotificationMock().Object);
        var result = await svc.CreateAsync(request, Guid.NewGuid());

        result.Title.Should().Be("New Event");
        db.Events.Count().Should().Be(1);
    }
}

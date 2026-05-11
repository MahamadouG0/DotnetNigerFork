using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Domain.Entities;
using DotnetNiger.Community.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Application.Services;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly EventService _eventService;

    public EventServiceTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _eventService = new EventService(_eventRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllEventsAsync_ReturnsPagedEvents()
    {
        // Arrange
        var events = new List<Event>
        {
            new() { Id = "1", Title = "Event 1", Description = "Desc 1", StartDate = DateTime.UtcNow },
            new() { Id = "2", Title = "Event 2", Description = "Desc 2", StartDate = DateTime.UtcNow }
        };

        _eventRepositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<Func<Event, bool>>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((events, events.Count));

        // Act
        var result = await _eventService.GetAllEventsAsync(page: 1, pageSize: 10);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEventByIdAsync_WithValidId_ReturnsEvent()
    {
        // Arrange
        var eventId = "1";
        var eventEntity = new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test",
            StartDate = DateTime.UtcNow
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(eventEntity);

        // Act
        var result = await _eventService.GetEventByIdAsync(eventId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Event");
    }

    [Fact]
    public async Task CreateEventAsync_WithValidRequest_CreatesEvent()
    {
        // Arrange
        var request = new CreateEventDto
        {
            Title = "New Event",
            Description = "Event Description",
            StartDate = DateTime.UtcNow.AddDays(1)
        };

        _eventRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _eventService.CreateEventAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Event");
        _eventRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEventAsync_WithValidId_UpdatesEvent()
    {
        // Arrange
        var eventId = "1";
        var existingEvent = new Event
        {
            Id = eventId,
            Title = "Old Title",
            Description = "Old Desc",
            StartDate = DateTime.UtcNow
        };

        var updateRequest = new UpdateEventDto
        {
            Title = "New Title",
            Description = "New Desc",
            StartDate = DateTime.UtcNow.AddDays(2)
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(existingEvent);

        _eventRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _eventService.UpdateEventAsync(eventId, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Title");
        _eventRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public async Task DeleteEventAsync_WithValidId_DeletesEvent()
    {
        // Arrange
        var eventId = "1";
        var eventEntity = new Event { Id = eventId, Title = "Test", Description = "Test", StartDate = DateTime.UtcNow };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(eventEntity);

        _eventRepositoryMock
            .Setup(x => x.DeleteAsync(eventEntity))
            .Returns(Task.CompletedTask);

        // Act
        await _eventService.DeleteEventAsync(eventId);

        // Assert
        _eventRepositoryMock.Verify(x => x.DeleteAsync(eventEntity), Times.Once);
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_ReturnsOnlyFutureEvents()
    {
        // Arrange
        var futureEvent = new Event { Id = "1", Title = "Future", Description = "Desc", StartDate = DateTime.UtcNow.AddDays(1) };
        var events = new List<Event> { futureEvent };

        _eventRepositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<Func<Event, bool>>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((events, events.Count));

        // Act
        var result = await _eventService.GetUpcomingEventsAsync();

        // Assert
        result.Should().HaveCount(1);
    }
}

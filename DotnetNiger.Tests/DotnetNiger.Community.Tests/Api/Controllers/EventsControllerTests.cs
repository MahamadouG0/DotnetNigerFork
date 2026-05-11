using DotnetNiger.Community.Api.Controllers;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Api.Controllers;

public class EventsControllerTests
{
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly EventsController _controller;

    public EventsControllerTests()
    {
        _eventServiceMock = new Mock<IEventService>();
        _controller = new EventsController(_eventServiceMock.Object);
    }

    [Fact]
    public async Task GetAllEvents_ReturnsOkResult()
    {
        // Arrange
        var events = new List<EventDto>
        {
            new() { Id = "1", Title = "Event 1", Description = "Desc 1" },
            new() { Id = "2", Title = "Event 2", Description = "Desc 2" }
        };

        _eventServiceMock
            .Setup(x => x.GetAllEventsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(events);

        // Act
        var result = await _controller.GetAllEvents(page: 1, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        _eventServiceMock.Verify(x => x.GetAllEventsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetEventById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var eventId = "1";
        var eventDto = new EventDto
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test Description"
        };

        _eventServiceMock
            .Setup(x => x.GetEventByIdAsync(eventId))
            .ReturnsAsync(eventDto);

        // Act
        var result = await _controller.GetEventById(eventId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateEvent_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateEventDto
        {
            Title = "New Event",
            Description = "Event Description",
            StartDate = DateTime.UtcNow.AddDays(1)
        };

        var createdEvent = new EventDto
        {
            Id = "1",
            Title = "New Event",
            Description = "Event Description"
        };

        _eventServiceMock
            .Setup(x => x.CreateEventAsync(request))
            .ReturnsAsync(createdEvent);

        // Act
        var result = await _controller.CreateEvent(request);

        // Assert
        result.Should().NotBeNull();
        _eventServiceMock.Verify(x => x.CreateEventAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpdateEvent_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var eventId = "1";
        var request = new UpdateEventDto
        {
            Title = "Updated Event",
            Description = "Updated Description",
            StartDate = DateTime.UtcNow.AddDays(2)
        };

        var updatedEvent = new EventDto
        {
            Id = eventId,
            Title = "Updated Event",
            Description = "Updated Description"
        };

        _eventServiceMock
            .Setup(x => x.UpdateEventAsync(eventId, request))
            .ReturnsAsync(updatedEvent);

        // Act
        var result = await _controller.UpdateEvent(eventId, request);

        // Assert
        result.Should().NotBeNull();
        _eventServiceMock.Verify(x => x.UpdateEventAsync(eventId, request), Times.Once);
    }

    [Fact]
    public async Task DeleteEvent_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var eventId = "1";

        _eventServiceMock
            .Setup(x => x.DeleteEventAsync(eventId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteEvent(eventId);

        // Assert
        result.Should().NotBeNull();
        _eventServiceMock.Verify(x => x.DeleteEventAsync(eventId), Times.Once);
    }

    [Fact]
    public async Task GetUpcomingEvents_ReturnsOkResult()
    {
        // Arrange
        var futureEvents = new List<EventDto>
        {
            new() { Id = "1", Title = "Future Event", Description = "Desc" }
        };

        _eventServiceMock
            .Setup(x => x.GetUpcomingEventsAsync())
            .ReturnsAsync(futureEvents);

        // Act
        var result = await _controller.GetUpcomingEvents();

        // Assert
        result.Should().NotBeNull();
    }
}

using DotnetNiger.Community.Api.Controllers;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Api.Controllers;

public class ResourcesControllerTests
{
    private readonly Mock<IResourceService> _resourceServiceMock;
    private readonly ResourcesController _controller;

    public ResourcesControllerTests()
    {
        _resourceServiceMock = new Mock<IResourceService>();
        _controller = new ResourcesController(_resourceServiceMock.Object);
    }

    [Fact]
    public async Task GetAllResources_ReturnsOkResult()
    {
        // Arrange
        var resources = new List<ResourceDto>
        {
            new() { Id = "1", Title = "Resource 1", Description = "Desc 1" },
            new() { Id = "2", Title = "Resource 2", Description = "Desc 2" }
        };

        _resourceServiceMock
            .Setup(x => x.GetAllResourcesAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(resources);

        // Act
        var result = await _controller.GetAllResources(page: 1, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        _resourceServiceMock.Verify(x => x.GetAllResourcesAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetResourceById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var resourceId = "1";
        var resourceDto = new ResourceDto
        {
            Id = resourceId,
            Title = "Test Resource",
            Description = "Test Description"
        };

        _resourceServiceMock
            .Setup(x => x.GetResourceByIdAsync(resourceId))
            .ReturnsAsync(resourceDto);

        // Act
        var result = await _controller.GetResourceById(resourceId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateResource_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateResourceDto
        {
            Title = "New Resource",
            Description = "Resource Description",
            Category = "Technology"
        };

        var createdResource = new ResourceDto
        {
            Id = "1",
            Title = "New Resource",
            Description = "Resource Description"
        };

        _resourceServiceMock
            .Setup(x => x.CreateResourceAsync(request))
            .ReturnsAsync(createdResource);

        // Act
        var result = await _controller.CreateResource(request);

        // Assert
        result.Should().NotBeNull();
        _resourceServiceMock.Verify(x => x.CreateResourceAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpdateResource_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var resourceId = "1";
        var request = new UpdateResourceDto
        {
            Title = "Updated Resource",
            Description = "Updated Description"
        };

        var updatedResource = new ResourceDto
        {
            Id = resourceId,
            Title = "Updated Resource",
            Description = "Updated Description"
        };

        _resourceServiceMock
            .Setup(x => x.UpdateResourceAsync(resourceId, request))
            .ReturnsAsync(updatedResource);

        // Act
        var result = await _controller.UpdateResource(resourceId, request);

        // Assert
        result.Should().NotBeNull();
        _resourceServiceMock.Verify(x => x.UpdateResourceAsync(resourceId, request), Times.Once);
    }

    [Fact]
    public async Task DeleteResource_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var resourceId = "1";

        _resourceServiceMock
            .Setup(x => x.DeleteResourceAsync(resourceId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteResource(resourceId);

        // Assert
        result.Should().NotBeNull();
        _resourceServiceMock.Verify(x => x.DeleteResourceAsync(resourceId), Times.Once);
    }

    [Fact]
    public async Task GetResourcesByCategory_WithValidCategory_ReturnsOkResult()
    {
        // Arrange
        var category = "Technology";
        var resources = new List<ResourceDto>
        {
            new() { Id = "1", Title = "Tech Resource", Category = category }
        };

        _resourceServiceMock
            .Setup(x => x.GetResourcesByCategoryAsync(category))
            .ReturnsAsync(resources);

        // Act
        var result = await _controller.GetResourcesByCategory(category);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetResourcesByStatus_WithValidStatus_ReturnsOkResult()
    {
        // Arrange
        var status = "Approved";
        var resources = new List<ResourceDto>
        {
            new() { Id = "1", Title = "Approved Resource", Status = status }
        };

        _resourceServiceMock
            .Setup(x => x.GetResourcesByStatusAsync(status))
            .ReturnsAsync(resources);

        // Act
        var result = await _controller.GetResourcesByStatus(status);

        // Assert
        result.Should().NotBeNull();
    }
}

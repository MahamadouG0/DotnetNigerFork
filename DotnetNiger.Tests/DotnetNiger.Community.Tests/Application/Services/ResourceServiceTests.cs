using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Domain.Entities;
using DotnetNiger.Community.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Application.Services;

public class ResourceServiceTests
{
    private readonly Mock<IResourceRepository> _resourceRepositoryMock;
    private readonly ResourceService _resourceService;

    public ResourceServiceTests()
    {
        _resourceRepositoryMock = new Mock<IResourceRepository>();
        _resourceService = new ResourceService(_resourceRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllResourcesAsync_ReturnsPagedResources()
    {
        // Arrange
        var resources = new List<Resource>
        {
            new() { Id = "1", Title = "Resource 1", Description = "Desc 1", CreatedAt = DateTime.UtcNow },
            new() { Id = "2", Title = "Resource 2", Description = "Desc 2", CreatedAt = DateTime.UtcNow }
        };

        _resourceRepositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<Func<Resource, bool>>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((resources, resources.Count));

        // Act
        var result = await _resourceService.GetAllResourcesAsync(page: 1, pageSize: 10);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetResourceByIdAsync_WithValidId_ReturnsResource()
    {
        // Arrange
        var resourceId = "1";
        var resource = new Resource
        {
            Id = resourceId,
            Title = "Test Resource",
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow
        };

        _resourceRepositoryMock
            .Setup(x => x.GetByIdAsync(resourceId))
            .ReturnsAsync(resource);

        // Act
        var result = await _resourceService.GetResourceByIdAsync(resourceId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Resource");
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidRequest_CreatesResource()
    {
        // Arrange
        var request = new CreateResourceDto
        {
            Title = "New Resource",
            Description = "Resource Description",
            Category = "Technology"
        };

        _resourceRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Resource>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _resourceService.CreateResourceAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Resource");
        _resourceRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Resource>()), Times.Once);
    }

    [Fact]
    public async Task UpdateResourceAsync_WithValidId_UpdatesResource()
    {
        // Arrange
        var resourceId = "1";
        var existingResource = new Resource
        {
            Id = resourceId,
            Title = "Old Title",
            Description = "Old Description",
            CreatedAt = DateTime.UtcNow
        };

        var updateRequest = new UpdateResourceDto
        {
            Title = "New Title",
            Description = "New Description"
        };

        _resourceRepositoryMock
            .Setup(x => x.GetByIdAsync(resourceId))
            .ReturnsAsync(existingResource);

        _resourceRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Resource>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _resourceService.UpdateResourceAsync(resourceId, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Title");
        _resourceRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Resource>()), Times.Once);
    }

    [Fact]
    public async Task DeleteResourceAsync_WithValidId_DeletesResource()
    {
        // Arrange
        var resourceId = "1";
        var resource = new Resource { Id = resourceId, Title = "Test", Description = "Test", CreatedAt = DateTime.UtcNow };

        _resourceRepositoryMock
            .Setup(x => x.GetByIdAsync(resourceId))
            .ReturnsAsync(resource);

        _resourceRepositoryMock
            .Setup(x => x.DeleteAsync(resource))
            .Returns(Task.CompletedTask);

        // Act
        await _resourceService.DeleteResourceAsync(resourceId);

        // Assert
        _resourceRepositoryMock.Verify(x => x.DeleteAsync(resource), Times.Once);
    }

    [Fact]
    public async Task GetResourcesByCategoryAsync_ReturnsFilteredResources()
    {
        // Arrange
        var category = "Technology";
        var resources = new List<Resource>
        {
            new() { Id = "1", Title = "Tech Resource", Category = category, CreatedAt = DateTime.UtcNow }
        };

        _resourceRepositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<Func<Resource, bool>>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((resources, resources.Count));

        // Act
        var result = await _resourceService.GetResourcesByCategoryAsync(category);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetResourcesByStatusAsync_ReturnsResourcesByStatus()
    {
        // Arrange
        var status = "Approved";
        var resources = new List<Resource>
        {
            new() { Id = "1", Title = "Approved Resource", Status = status, CreatedAt = DateTime.UtcNow }
        };

        _resourceRepositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<Func<Resource, bool>>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((resources, resources.Count));

        // Act
        var result = await _resourceService.GetResourcesByStatusAsync(status);

        // Assert
        result.Should().HaveCount(1);
    }
}

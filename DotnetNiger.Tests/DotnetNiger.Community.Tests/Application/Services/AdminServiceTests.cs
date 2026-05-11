using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Domain.Entities;
using DotnetNiger.Community.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Application.Services;

public class AdminServiceTests
{
    private readonly Mock<IPostRepository> _postRepositoryMock;
    private readonly Mock<ICommentRepository> _commentRepositoryMock;
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IResourceRepository> _resourceRepositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly AdminService _adminService;

    public AdminServiceTests()
    {
        _postRepositoryMock = new Mock<IPostRepository>();
        _commentRepositoryMock = new Mock<ICommentRepository>();
        _eventRepositoryMock = new Mock<IEventRepository>();
        _resourceRepositoryMock = new Mock<IResourceRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();

        _adminService = new AdminService(
            _postRepositoryMock.Object,
            _commentRepositoryMock.Object,
            _eventRepositoryMock.Object,
            _resourceRepositoryMock.Object,
            _projectRepositoryMock.Object);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsValidDashboard()
    {
        // Arrange
        _postRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(10);
        _commentRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(5);
        _eventRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(3);
        _resourceRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(8);
        _projectRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(2);

        // Act
        var result = await _adminService.GetDashboardAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalPosts.Should().Be(10);
        result.TotalComments.Should().Be(5);
        result.TotalEvents.Should().Be(3);
        result.TotalResources.Should().Be(8);
        result.TotalProjects.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboardAsync_UsesCountAsyncNotGetAllAsync()
    {
        // Arrange
        _postRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(5);
        _commentRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(2);
        _eventRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(1);
        _resourceRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(3);
        _projectRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(0);

        // Act
        await _adminService.GetDashboardAsync();

        // Assert
        _postRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _commentRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _eventRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _resourceRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _projectRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
    }

    [Fact]
    public async Task GetDashboardAsync_WithZeroCounts_ReturnsZero()
    {
        // Arrange
        _postRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(0);
        _commentRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(0);
        _eventRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(0);
        _resourceRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(0);
        _projectRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(0);

        // Act
        var result = await _adminService.GetDashboardAsync();

        // Assert
        result.TotalPosts.Should().Be(0);
        result.TotalComments.Should().Be(0);
        result.TotalEvents.Should().Be(0);
        result.TotalResources.Should().Be(0);
        result.TotalProjects.Should().Be(0);
    }

    [Fact]
    public async Task GetDashboardAsync_WithLargeCounts_ReturnsCorrectly()
    {
        // Arrange
        var largeCount = 10000;
        _postRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(largeCount);
        _commentRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(largeCount);
        _eventRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(largeCount);
        _resourceRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(largeCount);
        _projectRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(largeCount);

        // Act
        var result = await _adminService.GetDashboardAsync();

        // Assert
        result.TotalPosts.Should().Be(largeCount);
        result.TotalComments.Should().Be(largeCount);
    }
}

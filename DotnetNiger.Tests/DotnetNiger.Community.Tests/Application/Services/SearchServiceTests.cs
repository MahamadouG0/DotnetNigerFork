using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Domain.Entities;
using DotnetNiger.Community.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Application.Services;

public class SearchServiceTests
{
    private readonly Mock<IPostRepository> _postRepositoryMock;
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IResourceRepository> _resourceRepositoryMock;
    private readonly SearchService _searchService;

    public SearchServiceTests()
    {
        _postRepositoryMock = new Mock<IPostRepository>();
        _eventRepositoryMock = new Mock<IEventRepository>();
        _resourceRepositoryMock = new Mock<IResourceRepository>();

        _searchService = new SearchService(
            _postRepositoryMock.Object,
            _eventRepositoryMock.Object,
            _resourceRepositoryMock.Object);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsSearchResults()
    {
        // Arrange
        var query = "test";
        var posts = new List<Post>
        {
            new() { Id = "1", Title = "Test Post", Content = "Test content", CreatedAt = DateTime.UtcNow }
        };

        _postRepositoryMock
            .Setup(x => x.SearchAsync(
                It.IsAny<Func<Post, bool>>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync(posts);

        // Act
        var result = await _searchService.SearchAsync(query, 1, 10);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().HaveCount(1);
        _postRepositoryMock.Verify(x => x.SearchAsync(It.IsAny<Func<Post, bool>>(), 1, 10), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmpty()
    {
        // Arrange
        var query = "";

        // Act
        var result = await _searchService.SearchAsync(query, 1, 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WithPagination_RespectsPageSize()
    {
        // Arrange
        var query = "test";
        var pageSize = 10;
        _postRepositoryMock
            .Setup(x => x.SearchAsync(It.IsAny<Func<Post, bool>>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Post>());

        // Act
        await _searchService.SearchAsync(query, 1, pageSize);

        // Assert
        _postRepositoryMock.Verify(
            x => x.SearchAsync(It.IsAny<Func<Post, bool>>(), 1, pageSize),
            Times.Once);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("")]
    public async Task SearchAsync_WithShortQuery_ReturnsEmpty(string shortQuery)
    {
        // Act
        var result = await _searchService.SearchAsync(shortQuery, 1, 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_CallsRepositorySearchAsync()
    {
        // Arrange
        var query = "test query";
        _postRepositoryMock
            .Setup(x => x.SearchAsync(It.IsAny<Func<Post, bool>>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Post>());

        // Act
        await _searchService.SearchAsync(query, 1, 20);

        // Assert
        _postRepositoryMock.Verify(
            x => x.SearchAsync(It.IsAny<Func<Post, bool>>(), 1, 20),
            Times.Once,
            "SearchAsync should be called with page 1 and pageSize 20");
    }
}

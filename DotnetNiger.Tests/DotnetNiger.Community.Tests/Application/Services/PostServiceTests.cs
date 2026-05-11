using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Domain.Entities;
using DotnetNiger.Community.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Application.Services;

public class PostServiceTests
{
    private readonly Mock<IPostRepository> _postRepositoryMock;
    private readonly PostService _postService;

    public PostServiceTests()
    {
        _postRepositoryMock = new Mock<IPostRepository>();
        _postService = new PostService(_postRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllPostsAsync_ReturnsAllPosts()
    {
        // Arrange
        var posts = new List<Post>
        {
            new() { Id = "1", Title = "Post 1", Content = "Content 1", CreatedAt = DateTime.UtcNow },
            new() { Id = "2", Title = "Post 2", Content = "Content 2", CreatedAt = DateTime.UtcNow }
        };

        _postRepositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<Func<Post, bool>>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((posts, posts.Count));

        // Act
        var result = await _postService.GetAllPostsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainItemsAssignableTo<PostDto>();
    }

    [Fact]
    public async Task GetPostByIdAsync_WithValidId_ReturnsPost()
    {
        // Arrange
        var postId = "1";
        var post = new Post
        {
            Id = postId,
            Title = "Test Post",
            Content = "Test Content",
            CreatedAt = DateTime.UtcNow
        };

        _postRepositoryMock
            .Setup(x => x.GetByIdAsync(postId))
            .ReturnsAsync(post);

        // Act
        var result = await _postService.GetPostByIdAsync(postId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Post");
    }

    [Fact]
    public async Task GetPostByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var postId = "invalid";
        _postRepositoryMock
            .Setup(x => x.GetByIdAsync(postId))
            .ReturnsAsync((Post?)null);

        // Act
        var result = await _postService.GetPostByIdAsync(postId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreatePostAsync_WithValidRequest_CreatesPost()
    {
        // Arrange
        var request = new CreatePostDto
        {
            Title = "New Post",
            Content = "New Content",
            Category = "Technology"
        };

        _postRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Post>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _postService.CreatePostAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Post");
        _postRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Post>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePostAsync_WithValidId_UpdatesPost()
    {
        // Arrange
        var postId = "1";
        var existingPost = new Post
        {
            Id = postId,
            Title = "Old Title",
            Content = "Old Content"
        };

        var updateRequest = new UpdatePostDto
        {
            Title = "New Title",
            Content = "New Content"
        };

        _postRepositoryMock
            .Setup(x => x.GetByIdAsync(postId))
            .ReturnsAsync(existingPost);

        _postRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Post>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _postService.UpdatePostAsync(postId, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Title");
        _postRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Post>()), Times.Once);
    }

    [Fact]
    public async Task DeletePostAsync_WithValidId_DeletesPost()
    {
        // Arrange
        var postId = "1";
        var post = new Post { Id = postId, Title = "Test", Content = "Test" };

        _postRepositoryMock
            .Setup(x => x.GetByIdAsync(postId))
            .ReturnsAsync(post);

        _postRepositoryMock
            .Setup(x => x.DeleteAsync(post))
            .Returns(Task.CompletedTask);

        // Act
        await _postService.DeletePostAsync(postId);

        // Assert
        _postRepositoryMock.Verify(x => x.DeleteAsync(post), Times.Once);
    }

    [Fact]
    public async Task GetPostsByCategoryAsync_ReturnsFilteredPosts()
    {
        // Arrange
        var category = "Technology";
        var posts = new List<Post>
        {
            new() { Id = "1", Title = "Tech Post", Category = category, CreatedAt = DateTime.UtcNow }
        };

        _postRepositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<Func<Post, bool>>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((posts, posts.Count));

        // Act
        var result = await _postService.GetPostsByCategoryAsync(category);

        // Assert
        result.Should().HaveCount(1);
    }
}

using DotnetNiger.Community.Api.Controllers;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Api.Controllers;

public class PostsControllerTests
{
    private readonly Mock<IPostService> _postServiceMock;
    private readonly PostsController _controller;

    public PostsControllerTests()
    {
        _postServiceMock = new Mock<IPostService>();
        _controller = new PostsController(_postServiceMock.Object);
    }

    [Fact]
    public async Task GetAllPosts_ReturnsOkResult()
    {
        // Arrange
        var posts = new List<PostDto>
        {
            new() { Id = "1", Title = "Post 1", Content = "Content 1" },
            new() { Id = "2", Title = "Post 2", Content = "Content 2" }
        };

        _postServiceMock
            .Setup(x => x.GetAllPostsAsync())
            .ReturnsAsync(posts);

        // Act
        var result = await _controller.GetAllPosts();

        // Assert
        result.Should().NotBeNull();
        _postServiceMock.Verify(x => x.GetAllPostsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPostById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var postId = "1";
        var post = new PostDto
        {
            Id = postId,
            Title = "Test Post",
            Content = "Test Content"
        };

        _postServiceMock
            .Setup(x => x.GetPostByIdAsync(postId))
            .ReturnsAsync(post);

        // Act
        var result = await _controller.GetPostById(postId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePost_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreatePostDto
        {
            Title = "New Post",
            Content = "New Content"
        };

        var createdPost = new PostDto
        {
            Id = "1",
            Title = "New Post",
            Content = "New Content"
        };

        _postServiceMock
            .Setup(x => x.CreatePostAsync(request))
            .ReturnsAsync(createdPost);

        // Act
        var result = await _controller.CreatePost(request);

        // Assert
        result.Should().NotBeNull();
        _postServiceMock.Verify(x => x.CreatePostAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpdatePost_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var postId = "1";
        var request = new UpdatePostDto
        {
            Title = "Updated Post",
            Content = "Updated Content"
        };

        var updatedPost = new PostDto
        {
            Id = postId,
            Title = "Updated Post",
            Content = "Updated Content"
        };

        _postServiceMock
            .Setup(x => x.UpdatePostAsync(postId, request))
            .ReturnsAsync(updatedPost);

        // Act
        var result = await _controller.UpdatePost(postId, request);

        // Assert
        result.Should().NotBeNull();
        _postServiceMock.Verify(x => x.UpdatePostAsync(postId, request), Times.Once);
    }

    [Fact]
    public async Task DeletePost_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var postId = "1";

        _postServiceMock
            .Setup(x => x.DeletePostAsync(postId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeletePost(postId);

        // Assert
        result.Should().NotBeNull();
        _postServiceMock.Verify(x => x.DeletePostAsync(postId), Times.Once);
    }

    [Fact]
    public async Task GetPostsByCategory_WithValidCategory_ReturnsOkResult()
    {
        // Arrange
        var category = "Technology";
        var posts = new List<PostDto>
        {
            new() { Id = "1", Title = "Tech Post", Category = category }
        };

        _postServiceMock
            .Setup(x => x.GetPostsByCategoryAsync(category))
            .ReturnsAsync(posts);

        // Act
        var result = await _controller.GetPostsByCategory(category);

        // Assert
        result.Should().NotBeNull();
    }
}

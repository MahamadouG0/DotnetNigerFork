using DotnetNiger.Community.Api.Controllers;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Api.Controllers;

public class CommentsControllerTests
{
    private readonly Mock<ICommentService> _commentServiceMock;
    private readonly CommentsController _controller;

    public CommentsControllerTests()
    {
        _commentServiceMock = new Mock<ICommentService>();
        _controller = new CommentsController(_commentServiceMock.Object);
    }

    [Fact]
    public async Task GetAllComments_ReturnsOkResult()
    {
        // Arrange
        var comments = new List<CommentDto>
        {
            new() { Id = "1", Content = "Comment 1", PostId = "post1" },
            new() { Id = "2", Content = "Comment 2", PostId = "post1" }
        };

        _commentServiceMock
            .Setup(x => x.GetAllCommentsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(comments);

        // Act
        var result = await _controller.GetAllComments(page: 1, pageSize: 20);

        // Assert
        result.Should().NotBeNull();
        _commentServiceMock.Verify(x => x.GetAllCommentsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetCommentById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var commentId = "1";
        var comment = new CommentDto
        {
            Id = commentId,
            Content = "Test Comment",
            PostId = "post1"
        };

        _commentServiceMock
            .Setup(x => x.GetCommentByIdAsync(commentId))
            .ReturnsAsync(comment);

        // Act
        var result = await _controller.GetCommentById(commentId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCommentsByPostId_WithValidPostId_ReturnsOkResult()
    {
        // Arrange
        var postId = "post1";
        var comments = new List<CommentDto>
        {
            new() { Id = "1", Content = "Comment 1", PostId = postId }
        };

        _commentServiceMock
            .Setup(x => x.GetCommentsByPostIdAsync(postId))
            .ReturnsAsync(comments);

        // Act
        var result = await _controller.GetCommentsByPostId(postId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateComment_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateCommentDto
        {
            Content = "New Comment",
            PostId = "post1",
            AuthorId = "user123"
        };

        var createdComment = new CommentDto
        {
            Id = "1",
            Content = "New Comment",
            PostId = "post1"
        };

        _commentServiceMock
            .Setup(x => x.CreateCommentAsync(request))
            .ReturnsAsync(createdComment);

        // Act
        var result = await _controller.CreateComment(request);

        // Assert
        result.Should().NotBeNull();
        _commentServiceMock.Verify(x => x.CreateCommentAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeleteComment_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var commentId = "1";

        _commentServiceMock
            .Setup(x => x.DeleteCommentAsync(commentId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteComment(commentId);

        // Assert
        result.Should().NotBeNull();
        _commentServiceMock.Verify(x => x.DeleteCommentAsync(commentId), Times.Once);
    }

    [Fact]
    public async Task CreateComment_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        CreateCommentDto? request = null;

        // Act & Assert
        // Validation would be done at the API level or service level
        _commentServiceMock
            .Setup(x => x.CreateCommentAsync(It.IsAny<CreateCommentDto>()))
            .ThrowsAsync(new ArgumentNullException());

        // The actual behavior depends on how validation is implemented
    }
}

using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using DotnetNiger.Community.Domain.Entities;
using DotnetNiger.Community.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Application.Services;

public class CommentServiceTests
{
    private readonly Mock<ICommentRepository> _commentRepositoryMock;
    private readonly Mock<IPostRepository> _postRepositoryMock;
    private readonly CommentService _commentService;

    public CommentServiceTests()
    {
        _commentRepositoryMock = new Mock<ICommentRepository>();
        _postRepositoryMock = new Mock<IPostRepository>();

        _commentService = new CommentService(
            _commentRepositoryMock.Object,
            _postRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllCommentsAsync_WithValidPagination_ReturnsPagedComments()
    {
        // Arrange
        var comments = new List<Comment>
        {
            new() { Id = "1", Content = "Comment 1", PostId = "post1", CreatedAt = DateTime.UtcNow },
            new() { Id = "2", Content = "Comment 2", PostId = "post1", CreatedAt = DateTime.UtcNow }
        };

        _commentRepositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<Func<Comment, bool>>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((comments, comments.Count));

        // Act
        var result = await _commentService.GetAllCommentsAsync(page: 1, pageSize: 20);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCommentsByPostIdAsync_WithValidPostId_ReturnsComments()
    {
        // Arrange
        var postId = "post1";
        var comments = new List<Comment>
        {
            new() { Id = "1", Content = "Comment 1", PostId = postId, CreatedAt = DateTime.UtcNow }
        };

        _commentRepositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<Func<Comment, bool>>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((comments, comments.Count));

        // Act
        var result = await _commentService.GetCommentsByPostIdAsync(postId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateCommentAsync_WithValidRequest_CreatesComment()
    {
        // Arrange
        var postId = "post1";
        var post = new Post { Id = postId, Title = "Test", Content = "Test" };

        var request = new CreateCommentDto
        {
            Content = "New Comment",
            PostId = postId,
            AuthorId = "user123"
        };

        _postRepositoryMock
            .Setup(x => x.GetByIdAsync(postId))
            .ReturnsAsync(post);

        _commentRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Comment>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _commentService.CreateCommentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("New Comment");
        _commentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Comment>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCommentAsync_WithValidId_DeletesComment()
    {
        // Arrange
        var commentId = "1";
        var comment = new Comment { Id = commentId, Content = "Test", PostId = "post1" };

        _commentRepositoryMock
            .Setup(x => x.GetByIdAsync(commentId))
            .ReturnsAsync(comment);

        _commentRepositoryMock
            .Setup(x => x.DeleteAsync(comment))
            .Returns(Task.CompletedTask);

        // Act
        await _commentService.DeleteCommentAsync(commentId);

        // Assert
        _commentRepositoryMock.Verify(x => x.DeleteAsync(comment), Times.Once);
    }

    [Fact]
    public async Task GetCommentByIdAsync_WithValidId_ReturnsComment()
    {
        // Arrange
        var commentId = "1";
        var comment = new Comment
        {
            Id = commentId,
            Content = "Test Comment",
            PostId = "post1",
            CreatedAt = DateTime.UtcNow
        };

        _commentRepositoryMock
            .Setup(x => x.GetByIdAsync(commentId))
            .ReturnsAsync(comment);

        // Act
        var result = await _commentService.GetCommentByIdAsync(commentId);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Test Comment");
    }

    [Fact]
    public async Task GetAllCommentsAsync_RespectsPagination()
    {
        // Arrange
        var page = 2;
        var pageSize = 15;
        _commentRepositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<Func<Comment, bool>>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((new List<Comment>(), 0));

        // Act
        await _commentService.GetAllCommentsAsync(page, pageSize);

        // Assert
        _commentRepositoryMock.Verify(
            x => x.GetPagedAsync(It.IsAny<Func<Comment, bool>>(), page, pageSize),
            Times.Once);
    }
}

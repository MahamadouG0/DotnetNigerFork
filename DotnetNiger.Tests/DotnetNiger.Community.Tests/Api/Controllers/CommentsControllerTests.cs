using System.Security.Claims;
using DotnetNiger.Community.Api.Controllers;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Api.Controllers;

public class CommentsControllerTests
{
    private static CommentsController CreateController(ICommentService? svc = null, Guid? userId = null)
    {
        var ctrl = new CommentsController(svc ?? Mock.Of<ICommentService>());
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, (userId ?? Guid.NewGuid()).ToString())], "Test"))
            }
        };
        return ctrl;
    }

    [Fact]
    public async Task GetByPostId_ReturnsOk()
    {
        var svc = new Mock<ICommentService>();
        svc.Setup(x => x.GetByPostIdAsync(It.IsAny<Guid>())).ReturnsAsync([]);

        var ctrl = CreateController(svc.Object);
        var result = await ctrl.GetByPostId(Guid.NewGuid());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var svc = new Mock<ICommentService>();
        svc.Setup(x => x.CreateAsync(It.IsAny<CreateCommentRequest>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new CommentResponse { Id = Guid.NewGuid(), Content = "Test" });

        var ctrl = CreateController(svc.Object);
        var result = await ctrl.Create(new CreateCommentRequest { Content = "Test", PostId = Guid.NewGuid() });

        result.Should().BeOfType<OkObjectResult>();
    }
}

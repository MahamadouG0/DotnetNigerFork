using DotnetNiger.Community.Api.Controllers;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Api.Controllers;

public class PostsControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var svc = new Mock<IPostService>();
        svc.Setup(x => x.GetAllAsync(null, null, null, null, 1, 10))
            .ReturnsAsync(new PaginatedResponse<PostResponse>
                { Items = [new PostResponse { Id = Guid.NewGuid(), Title = "P1" }], TotalCount = 1, Page = 1, PageSize = 10 });

        var ctrl = new PostsController(svc.Object);
        var result = await ctrl.GetAll(null, null, null, null, 1, 10);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var svc = new Mock<IPostService>();
        svc.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((PostResponse?)null);

        var ctrl = new PostsController(svc.Object);
        var result = await ctrl.GetById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}

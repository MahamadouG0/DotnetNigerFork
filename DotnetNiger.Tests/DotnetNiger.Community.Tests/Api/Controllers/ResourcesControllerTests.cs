using DotnetNiger.Community.Api.Controllers;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DotnetNiger.Community.Tests.Api.Controllers;

public class ResourcesControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var svc = new Mock<IResourceService>();
        svc.Setup(x => x.GetAllAsync(null, null, null, null, null, 1, 10))
            .ReturnsAsync(new PaginatedResponse<ResourceResponse> { Items = [], TotalCount = 0, Page = 1, PageSize = 10 });

        var ctrl = new ResourcesController(svc.Object);
        var result = await ctrl.GetAll(null, null, null, null, null, 1, 10);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var svc = new Mock<IResourceService>();
        svc.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ResourceResponse?)null);

        var ctrl = new ResourcesController(svc.Object);
        var result = await ctrl.GetById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}

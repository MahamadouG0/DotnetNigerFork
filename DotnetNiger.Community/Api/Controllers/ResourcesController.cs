using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetNiger.Community.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ResourcesController(IResourceService resourceService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? resourceType, [FromQuery] string? level, [FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await resourceService.GetAllAsync(resourceType, level, query, page, pageSize);
        return Ok(new { Success = true, Data = result });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var resource = await resourceService.GetByIdAsync(id);
        if (resource is null) return NotFound(new { Success = false, Message = "Resource not found" });
        return Ok(new { Success = true, Data = resource });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateResourceRequest request)
    {
        var resource = await resourceService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = resource.Id }, new { Success = true, Data = resource });
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateResourceRequest request)
    {
        var resource = await resourceService.UpdateAsync(id, request);
        if (resource is null) return NotFound(new { Success = false, Message = "Resource not found" });
        return Ok(new { Success = true, Data = resource });
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await resourceService.DeleteAsync(id);
        if (!deleted) return NotFound(new { Success = false, Message = "Resource not found" });
        return Ok(new { Success = true, Message = "Resource deleted" });
    }

    [HttpPost("{id:guid}/views")]
    public async Task<IActionResult> IncrementViewCount(Guid id)
    {
        var resource = await resourceService.IncrementViewCountAsync(id);
        if (resource is null) return NotFound(new { Success = false, Message = "Resource not found" });
        return Ok(new { Success = true, Data = resource });
    }
}

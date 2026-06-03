using Asp.Versioning;
using System.Security.Claims;
using DotnetNiger.Community.Application;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetNiger.Community.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PostsController(IPostService postService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? published, [FromQuery] string? category, [FromQuery] string? tag, [FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, ValidationConstants.MaxPageSize);
        var result = await postService.GetAllAsync(published, category, tag, query, page, pageSize);
        return Ok(new { Success = true, Data = result });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var post = await postService.GetByIdAsync(id);
        if (post is null) return NotFound(new { Success = false, Message = "Post not found" });
        return Ok(new { Success = true, Data = post });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { Success = false, Message = "Invalid user identity" });

        var userName = User.FindFirstValue("full_name") ?? User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
        var post = await postService.CreateAsync(request, userId, userName);
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, new { Success = true, Data = post });
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePostRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { Success = false, Message = "Invalid user identity" });

        var isAdmin = User.IsInRole("Admin");
        var post = await postService.UpdateAsync(id, request, userId, isAdmin);
        if (post is null) return NotFound(new { Success = false, Message = "Post not found" });
        return Ok(new { Success = true, Data = post });
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { Success = false, Message = "Invalid user identity" });

        var isAdmin = User.IsInRole("Admin");
        var deleted = await postService.DeleteAsync(id, userId, isAdmin);
        if (!deleted) return NotFound(new { Success = false, Message = "Post not found" });
        return Ok(new { Success = true, Message = "Post deleted" });
    }
}

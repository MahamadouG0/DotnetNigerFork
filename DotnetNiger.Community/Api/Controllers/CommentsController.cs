using Asp.Versioning;
using System.Security.Claims;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetNiger.Community.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CommentsController(ICommentService commentService) : ControllerBase
{
    [HttpGet("post/{postId:guid}")]
    public async Task<IActionResult> GetByPostId(Guid postId)
    {
        var comments = await commentService.GetByPostIdAsync(postId);
        return Ok(new { Success = true, Data = comments });
    }

    [HttpGet("event/{eventId:guid}")]
    public async Task<IActionResult> GetByEventId(Guid eventId)
    {
        var comments = await commentService.GetByEventIdAsync(eventId);
        return Ok(new { Success = true, Data = comments });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var comment = await commentService.GetByIdAsync(id);
        if (comment is null) return NotFound(new { Success = false, Message = "Comment not found" });
        return Ok(new { Success = true, Data = comment });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateCommentRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { Success = false, Message = "Invalid user identity" });

        var userName = User.FindFirstValue("full_name") ?? "Unknown";
        var avatar = User.FindFirstValue("avatar_url") ?? "";
        var comment = await commentService.CreateAsync(request, userId, userName, avatar);
        return Ok(new { Success = true, Data = comment });
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCommentRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { Success = false, Message = "Invalid user identity" });

        var comment = await commentService.UpdateAsync(id, request, userId);
        if (comment is null) return NotFound(new { Success = false, Message = "Comment not found" });
        return Ok(new { Success = true, Data = comment });
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] bool deleteAllReplies = false)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { Success = false, Message = "Invalid user identity" });

        var deleted = await commentService.DeleteAsync(id, userId, deleteAllReplies);
        if (!deleted) return NotFound(new { Success = false, Message = "Comment not found" });
        return Ok(new { Success = true, Message = "Comment deleted" });
    }
}

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
public class ProjectsController(IProjectService projectService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, ValidationConstants.MaxPageSize);
        var result = await projectService.GetAllAsync(status, query, page, pageSize);
        return Ok(new { Success = true, Data = result });
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured()
    {
        var projects = await projectService.GetFeaturedAsync();
        return Ok(new { Success = true, Data = projects });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var project = await projectService.GetByIdAsync(id);
        if (project is null) return NotFound(new { Success = false, Message = "Projet non trouvé" });
        return Ok(new { Success = true, Data = project });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { Success = false, Message = "Invalid user identity" });

        var authorName = User.FindFirstValue("full_name") ?? User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
        var project = await projectService.CreateAsync(request, userId, authorName);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, new { Success = true, Data = project });
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { Success = false, Message = "Invalid user identity" });

        var isAdmin = User.IsInRole("Admin");
        var project = await projectService.UpdateAsync(id, request, userId, isAdmin);
        if (project is null) return NotFound(new { Success = false, Message = "Projet non trouvé" });
        return Ok(new { Success = true, Data = project });
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { Success = false, Message = "Invalid user identity" });

        var isAdmin = User.IsInRole("Admin");
        var deleted = await projectService.DeleteAsync(id, userId, isAdmin);
        if (!deleted) return NotFound(new { Success = false, Message = "Projet non trouvé" });
        return Ok(new { Success = true, Message = "Projet supprimé" });
    }
}

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
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? published, [FromQuery] string? past, [FromQuery] string? eventType,
        [FromQuery] string? query, [FromQuery] string? tag,
        [FromQuery] DateTime? startDateFrom, [FromQuery] DateTime? startDateTo,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, ValidationConstants.MaxPageSize);
        var result = await eventService.GetAllAsync(published, past, eventType, query, tag, startDateFrom, startDateTo, page, pageSize);
        return Ok(new { Success = true, Data = result });
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, ValidationConstants.MaxPageSize);
        var events = await eventService.GetUpcomingAsync(page, pageSize);
        return Ok(new { Success = true, Data = events });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var ev = await eventService.GetByIdAsync(id);
        if (ev is null) return NotFound(new { Success = false, Message = "Event not found" });
        return Ok(new { Success = true, Data = ev });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { Success = false, Message = "Invalid user identity" });

        var ev = await eventService.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = ev.Id }, new { Success = true, Data = ev });
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateEventRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { Success = false, Message = "Invalid user identity" });

        var isAdmin = User.IsInRole("Admin");
        var ev = await eventService.UpdateAsync(id, request, userId, isAdmin);
        if (ev is null) return NotFound(new { Success = false, Message = "Event not found" });
        return Ok(new { Success = true, Data = ev });
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { Success = false, Message = "Invalid user identity" });

        var isAdmin = User.IsInRole("Admin");
        var deleted = await eventService.DeleteAsync(id, userId, isAdmin);
        if (!deleted) return NotFound(new { Success = false, Message = "Event not found" });
        return Ok(new { Success = true, Message = "Event deleted" });
    }

    [HttpPost("registrations")]
    [Authorize]
    public async Task<IActionResult> Register([FromBody] RegisterEventRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { Success = false, Message = "Invalid user identity" });

        var userName = User.FindFirstValue("full_name") ?? "Unknown";
        var result = await eventService.RegisterAsync(request.EventId, userId, userName);
        if (result is null)
            return BadRequest(new { Success = false, Message = "Event is full or already registered" });
        return Ok(new { Success = true, Data = result });
    }

    [HttpDelete("{eventId:guid}/registrations")]
    [Authorize]
    public async Task<IActionResult> CancelRegistration(Guid eventId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { Success = false, Message = "Invalid user identity" });

        var cancelled = await eventService.CancelRegistrationAsync(eventId, userId);
        if (!cancelled) return NotFound(new { Success = false, Message = "Registration not found" });
        return Ok(new { Success = true, Message = "Registration cancelled" });
    }

    [HttpGet("{eventId:guid}/registrations")]
    [Authorize]
    public async Task<IActionResult> GetRegistrations(Guid eventId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { Success = false, Message = "Invalid user identity" });

        var registrations = await eventService.GetRegistrationsAsync(eventId);
        return Ok(new { Success = true, Data = registrations });
    }
}

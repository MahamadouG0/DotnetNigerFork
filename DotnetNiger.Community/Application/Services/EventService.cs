using DotnetNiger.Community.Infrastructure;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Notifications;
using DotnetNiger.Community.Domain;
using DotnetNiger.Community.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Community.Application.Services;

public class EventService(AppDbContext db, INotificationService notificationService) : IEventService
{
    public async Task<PaginatedResponse<EventResponse>> GetAllAsync(string? published, string? past, string? eventType, string? query, string? tag, DateTime? startDateFrom, DateTime? startDateTo, int page = 1, int pageSize = 10)
    {
        var q = db.Events
            .Include(e => e.Medias)
            .Include(e => e.EventTags).ThenInclude(et => et.Tag)
            .AsSplitQuery()
            .AsQueryable();

        if (published == "true") q = q.Where(e => e.IsPublished);
        if (published == "false") q = q.Where(e => !e.IsPublished);
        if (past == "true") q = q.Where(e => e.EndDate < DateTime.UtcNow);
        if (past == "false") q = q.Where(e => e.EndDate >= DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(eventType)) q = q.Where(e => e.EventType == eventType);
        if (!string.IsNullOrWhiteSpace(tag))
            q = q.Where(e => e.EventTags.Any(et => et.Tag.Slug == tag));
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(e => e.Title.Contains(query) || e.Description.Contains(query));
        if (startDateFrom.HasValue)
            q = q.Where(e => e.StartDate >= startDateFrom.Value);
        if (startDateTo.HasValue)
            q = q.Where(e => e.StartDate <= startDateTo.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(e => e.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => MapEvent(e))
            .ToListAsync();

        return new PaginatedResponse<EventResponse> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<List<EventResponse>> GetUpcomingAsync(int page = 1, int pageSize = 10)
    {
        return await db.Events
            .Include(e => e.Medias)
            .Include(e => e.EventTags).ThenInclude(et => et.Tag)
            .Where(e => e.IsPublished && e.EndDate >= DateTime.UtcNow)
            .OrderBy(e => e.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => MapEvent(e))
            .ToListAsync();
    }

    public async Task<EventResponse?> GetByIdAsync(Guid id)
    {
        var ev = await db.Events
            .Include(e => e.Medias)
            .Include(e => e.EventTags).ThenInclude(et => et.Tag)
            .FirstOrDefaultAsync(e => e.Id == id);
        return ev is null ? null : MapEvent(ev);
    }

    public async Task<EventResponse?> GetBySlugAsync(string slug)
    {
        var ev = await db.Events
            .Include(e => e.Medias)
            .Include(e => e.EventTags).ThenInclude(et => et.Tag)
            .FirstOrDefaultAsync(e => e.Slug == slug);
        return ev is null ? null : MapEvent(ev);
    }

    public async Task<EventResponse> CreateAsync(CreateEventRequest request, Guid userId)
    {
        var ev = new Event
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = GenerateSlug(request.Title),
            Description = request.Description,
            Location = request.Location,
            EventType = request.EventType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CoverImageUrl = request.CoverImageUrl,
            CreatedBy = userId,
            Capacity = request.Capacity,
            MeetupLink = request.MeetupLink,
            IsPublished = request.IsPublished,
            IsArchived = request.IsArchived,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await AssignTags(ev, request.TagNames);
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        _ = notificationService.NotifyNewEventAsync(ev.Title, ev.Description, ev.StartDate);
        return MapEvent(ev);
    }

    public async Task<EventResponse?> UpdateAsync(Guid id, CreateEventRequest request, Guid userId, bool isAdmin)
    {
        var ev = await db.Events
            .Include(e => e.Medias)
            .Include(e => e.EventTags)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (ev is null) return null;
        if (ev.CreatedBy != userId && !isAdmin)
            throw new UnauthorizedAccessException("Vous n'êtes pas autorisé à modifier cet événement.");

        ev.Title = request.Title;
        ev.Slug = GenerateSlug(request.Title);
        ev.Description = request.Description;
        ev.Location = request.Location;
        ev.EventType = request.EventType;
        ev.StartDate = request.StartDate;
        ev.EndDate = request.EndDate;
        ev.CoverImageUrl = request.CoverImageUrl;
        ev.Capacity = request.Capacity;
        ev.MeetupLink = request.MeetupLink;
        ev.IsPublished = request.IsPublished;
        ev.IsArchived = request.IsArchived;
        ev.UpdatedAt = DateTime.UtcNow;

        db.EventTags.RemoveRange(ev.EventTags);
        await AssignTags(ev, request.TagNames);
        await db.SaveChangesAsync();
        return MapEvent(ev);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, bool isAdmin)
    {
        var ev = await db.Events.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id);
        if (ev is null) return false;
        if (ev.CreatedBy != userId && !isAdmin)
            throw new UnauthorizedAccessException("Vous n'êtes pas autorisé à supprimer cet événement.");
        ev.IsDeleted = true;
        ev.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<EventResponse?> PublishAsync(Guid id)
    {
        var ev = await db.Events.Include(e => e.Medias).FirstOrDefaultAsync(e => e.Id == id);
        if (ev is null) return null;
        ev.IsPublished = true;
        ev.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return MapEvent(ev);
    }

    public async Task<EventResponse?> UnpublishAsync(Guid id)
    {
        var ev = await db.Events.Include(e => e.Medias).FirstOrDefaultAsync(e => e.Id == id);
        if (ev is null) return null;
        ev.IsPublished = false;
        ev.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return MapEvent(ev);
    }

    public async Task<EventRegistrationResponse?> RegisterAsync(Guid eventId, Guid userId, string userName)
    {
        var existing = await db.EventRegistrations.AnyAsync(r => r.EventId == eventId && r.UserId == userId);
        if (existing) return null;

        var rows = await db.Database.ExecuteSqlRawAsync(
            "UPDATE Events SET RegisteredCount = RegisteredCount + 1 WHERE Id = ? AND RegisteredCount < Capacity",
            eventId);

        if (rows == 0) return null;

        var ev = await db.Events.FindAsync(eventId);

        var registration = new EventRegistration
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            UserName = userName,
            RegisteredAt = DateTime.UtcNow,
            RegistrationStatus = "Confirmed"
        };

        db.EventRegistrations.Add(registration);
        await db.SaveChangesAsync();

        return MapRegistration(registration, ev!.Title);
    }

    public async Task<bool> CancelRegistrationAsync(Guid eventId, Guid userId)
    {
        var reg = await db.EventRegistrations.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
        if (reg is null) return false;

        var ev = await db.Events.FindAsync(eventId);
        if (ev is not null) ev.RegisteredCount--;

        db.EventRegistrations.Remove(reg);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<EventRegistrationResponse>> GetRegistrationsAsync(Guid eventId)
    {
        return await db.EventRegistrations
            .Where(r => r.EventId == eventId)
            .Select(r => MapRegistration(r, ""))
            .ToListAsync();
    }

    private async Task AssignTags(Event ev, List<string> tagNames)
    {
        foreach (var name in tagNames.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            var slug = GenerateSlug(name);
            var tag = await db.Tags.FirstOrDefaultAsync(t => t.Slug == slug);
            if (tag is null)
            {
                tag = new Tag { Id = Guid.NewGuid(), Name = name, Slug = slug };
                db.Tags.Add(tag);
            }
            ev.EventTags.Add(new EventTag { EventId = ev.Id, TagId = tag.Id });
        }
    }

    private static EventResponse MapEvent(Event e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Slug = e.Slug,
        Description = e.Description,
        Location = e.Location,
        EventType = e.EventType,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        CoverImageUrl = e.CoverImageUrl,
        CreatedBy = e.CreatedBy,
        OrganizerName = e.OrganizerName,
        Capacity = e.Capacity,
        RegisteredCount = e.RegisteredCount,
        IsPublished = e.IsPublished,
        IsArchived = e.IsArchived,
        MeetupLink = e.MeetupLink,
        CreatedAt = e.CreatedAt,
        Medias = e.Medias.Select(m => new EventMediaResponse
        {
            Id = m.Id,
            Type = m.Type,
            Url = m.Url,
            Title = m.Title
        }).ToList(),
        Tags = e.EventTags.Select(et => new TagResponse
        {
            Id = et.Tag.Id,
            Name = et.Tag.Name,
            Slug = et.Tag.Slug,
            UsageCount = et.Tag.UsageCount
        }).ToList()
    };

    private static EventRegistrationResponse MapRegistration(EventRegistration r, string eventTitle) => new()
    {
        Id = r.Id,
        EventId = r.EventId,
        EventTitle = eventTitle,
        UserId = r.UserId,
        UserName = r.UserName,
        RegisteredAt = r.RegisteredAt,
        IsAttended = r.IsAttended,
        RegistrationStatus = r.RegistrationStatus
    };

    private static string GenerateSlug(string text) => SlugGenerator.Generate(text);
}

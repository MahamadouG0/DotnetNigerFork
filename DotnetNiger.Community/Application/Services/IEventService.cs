using DotnetNiger.Community.Application.DTOs;

namespace DotnetNiger.Community.Application.Services;

public interface IEventService
{
    Task<PaginatedResponse<EventResponse>> GetAllAsync(string? published, string? past, string? eventType, string? query, string? tag, DateTime? startDateFrom, DateTime? startDateTo, int page = 1, int pageSize = 10);
    Task<List<EventResponse>> GetUpcomingAsync(int page = 1, int pageSize = 10);
    Task<EventResponse?> GetByIdAsync(Guid id);
    Task<EventResponse?> GetBySlugAsync(string slug);
    Task<EventResponse> CreateAsync(CreateEventRequest request, Guid userId);
    Task<EventResponse?> UpdateAsync(Guid id, CreateEventRequest request, Guid userId, bool isAdmin);
    Task<bool> DeleteAsync(Guid id, Guid userId, bool isAdmin);
    Task<EventResponse?> PublishAsync(Guid id);
    Task<EventResponse?> UnpublishAsync(Guid id);
    Task<EventRegistrationResponse?> RegisterAsync(Guid eventId, Guid userId, string userName);
    Task<bool> CancelRegistrationAsync(Guid eventId, Guid userId);
    Task<List<EventRegistrationResponse>> GetRegistrationsAsync(Guid eventId);
}

namespace DotnetNiger.Community.Application.DTOs;

public class EventResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string CoverImageUrl { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int RegisteredCount { get; set; }
    public bool IsPublished { get; set; }
    public bool IsArchived { get; set; }
    public string MeetupLink { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<EventMediaResponse> Medias { get; set; } = [];
    public List<TagResponse> Tags { get; set; } = [];
}

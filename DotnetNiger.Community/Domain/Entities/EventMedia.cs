namespace DotnetNiger.Community.Domain.Entities;

public class EventMedia
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public Event Event { get; set; } = null!;
}

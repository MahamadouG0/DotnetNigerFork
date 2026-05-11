namespace DotnetNiger.Community.Domain.Entities;

public class EventRegistration
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public bool IsAttended { get; set; }
    public string RegistrationStatus { get; set; } = string.Empty;

    public Event Event { get; set; } = null!;
}

namespace DotnetNiger.Community.Application.DTOs;

public class EventRegistrationResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public bool IsAttended { get; set; }
    public string RegistrationStatus { get; set; } = string.Empty;
}

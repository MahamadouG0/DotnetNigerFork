namespace DotnetNiger.Community.Application.Notifications;

public interface INotificationService
{
    Task NotifyNewEventAsync(string title, string description, DateTime startDate);
    Task NotifyNewProjectAsync(string title, string description, string authorName);
}

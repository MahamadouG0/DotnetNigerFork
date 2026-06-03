using DotnetNiger.Community.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotnetNiger.Community.Application.Notifications;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task NotifyNewEventAsync(string title, string description, DateTime startDate)
    {
        var subscribers = await _db.NewsletterSubscriptions
            .Where(s => s.IsActive)
            .ToListAsync();

        _logger.LogInformation(
            "[NOTIFICATION] Nouvel événement créé: {Title} ({Count} abonnés notifiés)",
            title, subscribers.Count);

        foreach (var sub in subscribers)
        {
            _logger.LogInformation(
                "[NOTIFICATION] Email à {Email}: Nouvel événement - {Title}",
                sub.Email, title);
        }
    }

    public async Task NotifyNewProjectAsync(string title, string description, string authorName)
    {
        var subscribers = await _db.NewsletterSubscriptions
            .Where(s => s.IsActive)
            .ToListAsync();

        _logger.LogInformation(
            "[NOTIFICATION] Nouveau projet créé: {Title} par {Author} ({Count} abonnés notifiés)",
            title, authorName, subscribers.Count);

        foreach (var sub in subscribers)
        {
            _logger.LogInformation(
                "[NOTIFICATION] Email à {Email}: Nouveau projet - {Title}",
                sub.Email, title);
        }
    }
}

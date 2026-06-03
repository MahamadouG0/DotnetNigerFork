using Microsoft.EntityFrameworkCore;

namespace DotnetNiger.Identity.Infrastructure;

public class GdprCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GdprCleanupService> _logger;

    public GdprCleanupService(IServiceScopeFactory scopeFactory, ILogger<GdprCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during GDPR cleanup");
            }
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var auditLogRetentionDays = int.TryParse(config["Gdpr:AuditLogRetentionDays"], out var days) ? days : 365;
        var consentRetentionDays = int.TryParse(config["Gdpr:ConsentRetentionDays"], out var consentDays) ? consentDays : 730;

        var cutoffAudit = DateTime.UtcNow.AddDays(-auditLogRetentionDays);
        var oldLogs = await db.AuditLogs.Where(a => a.CreatedAt < cutoffAudit).CountAsync(ct);
        if (oldLogs > 0)
        {
            await db.AuditLogs.Where(a => a.CreatedAt < cutoffAudit).ExecuteDeleteAsync(ct);
            _logger.LogInformation("GDPR cleanup: deleted {Count} old audit logs", oldLogs);
        }

        var cutoffConsent = DateTime.UtcNow.AddDays(-consentRetentionDays);
        var oldConsents = await db.UserConsents.Where(c => c.CreatedAt < cutoffConsent).CountAsync(ct);
        if (oldConsents > 0)
        {
            await db.UserConsents.Where(c => c.CreatedAt < cutoffConsent).ExecuteDeleteAsync(ct);
            _logger.LogInformation("GDPR cleanup: deleted {Count} old consent records", oldConsents);
        }
    }
}

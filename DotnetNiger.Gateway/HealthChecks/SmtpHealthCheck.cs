using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotnetNiger.Gateway.HealthChecks;

public class SmtpHealthCheck : IHealthCheck
{
    private readonly string _host;
    private readonly int _port;

    public SmtpHealthCheck(IConfiguration configuration)
    {
        _host = configuration["Smtp:Host"] ?? "localhost";
        _port = int.TryParse(configuration["Smtp:Port"], out var p) ? p : 587;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_host) || _host == "localhost")
            return HealthCheckResult.Healthy("SMTP non configuré (localhost, ignoré)");

        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            await client.ConnectAsync(_host, _port, cts.Token);
            return HealthCheckResult.Healthy($"SMTP {_host}:{_port} joignable");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded($"SMTP {_host}:{_port} — timeout 5s");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"SMTP {_host}:{_port} injoignable", ex);
        }
    }
}

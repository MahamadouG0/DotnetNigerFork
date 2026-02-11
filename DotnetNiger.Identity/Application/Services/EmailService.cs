// Service applicatif Identity: EmailService
using DotnetNiger.Identity.Application.Services.Interfaces;
using DotnetNiger.Identity.Infrastructure.External;
using Microsoft.Extensions.Configuration;

namespace DotnetNiger.Identity.Application.Services;

// Envoi d'emails via SMTP, SendGrid ou Mailgun.
public class EmailService : IEmailService
{
	private readonly IConfiguration _configuration;
	private readonly IReadOnlyDictionary<string, IEmailProvider> _providers;

	public EmailService(IConfiguration configuration, IEnumerable<IEmailProvider> providers)
	{
		_configuration = configuration;
		_providers = providers.ToDictionary(provider => provider.Name, StringComparer.OrdinalIgnoreCase);
	}

	public async Task SendAsync(string to, string subject, string body)
	{
		var section = _configuration.GetSection("Email");
		var enabled = section.GetValue("Enabled", false);
		if (!enabled)
		{
			return;
		}

		var providerName = section.GetValue<string>("Provider")?.Trim() ?? "smtp";
		if (!_providers.TryGetValue(providerName, out var provider))
		{
			throw new InvalidOperationException("Email provider not supported.");
		}

		await provider.SendAsync(to, subject, body);
	}
}

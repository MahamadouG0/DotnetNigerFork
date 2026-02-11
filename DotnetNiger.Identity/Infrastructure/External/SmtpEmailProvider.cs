// Integration externe Identity: SmtpEmailProvider
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace DotnetNiger.Identity.Infrastructure.External;

// Provider SMTP.
public class SmtpEmailProvider : IEmailProvider
{
	private readonly IConfiguration _configuration;

	public SmtpEmailProvider(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public string Name => "smtp";

	public async Task SendAsync(string to, string subject, string body)
	{
		var section = _configuration.GetSection("Email").GetSection("Smtp");
		var host = section.GetValue<string>("Host");
		var from = section.GetValue<string>("From");
		var username = section.GetValue<string>("Username");
		var password = section.GetValue<string>("Password");
		var port = section.GetValue("Port", 587);
		var enableSsl = section.GetValue("EnableSsl", true);

		if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
		{
			throw new InvalidOperationException("SMTP settings are missing (Host/From). ");
		}

		using var message = new MailMessage(from, to)
		{
			Subject = subject,
			Body = body,
			IsBodyHtml = false
		};

		using var client = new SmtpClient(host, port)
		{
			EnableSsl = enableSsl
		};

		if (!string.IsNullOrWhiteSpace(username))
		{
			client.Credentials = new NetworkCredential(username, password);
		}

		await client.SendMailAsync(message);
	}
}

// Integration externe Identity: SendGridProvider
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace DotnetNiger.Identity.Infrastructure.External;

// Provider SendGrid.
public class SendGridProvider : IEmailProvider
{
	private readonly IConfiguration _configuration;
	private readonly IHttpClientFactory _httpClientFactory;

	public SendGridProvider(IConfiguration configuration, IHttpClientFactory httpClientFactory)
	{
		_configuration = configuration;
		_httpClientFactory = httpClientFactory;
	}

	public string Name => "sendgrid";

	public async Task SendAsync(string to, string subject, string body)
	{
		var section = _configuration.GetSection("Email").GetSection("SendGrid");
		var apiKey = section.GetValue<string>("ApiKey");
		var from = section.GetValue<string>("From");

		if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(from))
		{
			throw new InvalidOperationException("SendGrid settings are missing (ApiKey/From). ");
		}

		var payload = new
		{
			personalizations = new[]
			{
				new
				{
					to = new[] { new { email = to } }
				}
			},
			from = new { email = from },
			subject,
			content = new[]
			{
				new { type = "text/plain", value = body }
			}
		};

		var json = JsonSerializer.Serialize(payload);
		using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send")
		{
			Content = new StringContent(json, Encoding.UTF8, "application/json")
		};
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

		var client = _httpClientFactory.CreateClient();
		var response = await client.SendAsync(request);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException("SendGrid send failed.");
		}
	}
}

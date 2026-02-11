// Integration externe Identity: MailgunProvider
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace DotnetNiger.Identity.Infrastructure.External;

// Provider Mailgun.
public class MailgunProvider : IEmailProvider
{
	private readonly IConfiguration _configuration;
	private readonly IHttpClientFactory _httpClientFactory;

	public MailgunProvider(IConfiguration configuration, IHttpClientFactory httpClientFactory)
	{
		_configuration = configuration;
		_httpClientFactory = httpClientFactory;
	}

	public string Name => "mailgun";

	public async Task SendAsync(string to, string subject, string body)
	{
		var section = _configuration.GetSection("Email").GetSection("Mailgun");
		var apiKey = section.GetValue<string>("ApiKey");
		var domain = section.GetValue<string>("Domain");
		var from = section.GetValue<string>("From");

		if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(from))
		{
			throw new InvalidOperationException("Mailgun settings are missing (ApiKey/Domain/From). ");
		}

		var client = _httpClientFactory.CreateClient();
		var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.mailgun.net/v3/{domain}/messages");
		request.Headers.Authorization = new AuthenticationHeaderValue(
			"Basic",
			Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{apiKey}")));

		request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["from"] = from,
			["to"] = to,
			["subject"] = subject,
			["text"] = body
		});

		var response = await client.SendAsync(request);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException("Mailgun send failed.");
		}
	}
}

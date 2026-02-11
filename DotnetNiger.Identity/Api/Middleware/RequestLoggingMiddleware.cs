// Middleware API Identity: RequestLoggingMiddleware
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DotnetNiger.Identity.Api.Middleware;

// Journalisation simple des requetes.
public class RequestLoggingMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<RequestLoggingMiddleware> _logger;

	public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task Invoke(HttpContext context)
	{
		var watch = Stopwatch.StartNew();
		await _next(context);
		watch.Stop();

		// Log structure pour faciliter la recherche et l'audit.
		_logger.LogInformation(
			"{Method} {Path} responded {StatusCode} in {Elapsed}ms",
			context.Request.Method,
			context.Request.Path,
			context.Response.StatusCode,
			watch.ElapsedMilliseconds);
	}
}

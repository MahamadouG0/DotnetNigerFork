// Middleware API Identity: ErrorHandlingMiddleware
using System.Net;
using System.Text.Json;
using DotnetNiger.Identity.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DotnetNiger.Identity.Api.Middleware;

// Middleware de gestion globale des erreurs.
public class ErrorHandlingMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<ErrorHandlingMiddleware> _logger;

	public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task Invoke(HttpContext context)
	{
		try
		{
			await _next(context);
		}
		catch (IdentityException ex)
		{
			_logger.LogWarning(ex, "Identity error");
			await WriteErrorAsync(context, ex.StatusCode, ex.Message);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unhandled error");
			await WriteErrorAsync(context, (int)HttpStatusCode.InternalServerError, "An unexpected error occurred.");
		}
	}

	private static Task WriteErrorAsync(HttpContext context, int statusCode, string message)
	{
		context.Response.ContentType = "application/json";
		context.Response.StatusCode = statusCode;
		var payload = JsonSerializer.Serialize(new
		{
			status = statusCode,
			error = message
		});
		return context.Response.WriteAsync(payload);
	}
}

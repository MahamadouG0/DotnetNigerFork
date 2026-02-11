// Extension API Identity: MiddlewareExtensions
using DotnetNiger.Identity.Api.Middleware;
using Microsoft.AspNetCore.Builder;

namespace DotnetNiger.Identity.Api.Extensions;

// Extensions pour les middlewares custom.
public static class MiddlewareExtensions
{
	public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
	{
		return app.UseMiddleware<RequestLoggingMiddleware>();
	}

	public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
	{
		return app.UseMiddleware<ErrorHandlingMiddleware>();
	}
}

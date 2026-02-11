// Middleware API Identity: JwtMiddleware
using Microsoft.AspNetCore.Http;

namespace DotnetNiger.Identity.Api.Middleware;

// Middleware reserve pour traitements JWT custom.
public class JwtMiddleware
{
	private readonly RequestDelegate _next;

	public JwtMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public Task Invoke(HttpContext context)
	{
		return _next(context);
	}
}

// Filtre API Identity: AuthorizeFilter
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DotnetNiger.Identity.Api.Filters;

// Filtre custom pour securite additionnelle (place-holder).
public class AuthorizeFilter : IAuthorizationFilter
{
	public void OnAuthorization(AuthorizationFilterContext context)
	{
		if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
		{
			context.Result = new UnauthorizedResult();
		}
	}
}

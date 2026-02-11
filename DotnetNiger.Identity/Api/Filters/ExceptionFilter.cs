// Filtre API Identity: ExceptionFilter
using DotnetNiger.Identity.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DotnetNiger.Identity.Api.Filters;

public class ExceptionFilter : IExceptionFilter
{
	// Conversion des exceptions metier en ProblemDetails.
	public void OnException(ExceptionContext context)
	{
		if (context.Exception is IdentityException identityException)
		{
			context.Result = new ObjectResult(new ProblemDetails
			{
				Status = identityException.StatusCode,
				Title = "Identity error",
				Detail = identityException.Message
			})
			{
				StatusCode = identityException.StatusCode
			};

			context.ExceptionHandled = true;
			return;
		}

		context.Result = new ObjectResult(new ProblemDetails
		{
			Status = 500,
			Title = "Server error",
			Detail = "An unexpected error occurred."
		})
		{
			StatusCode = 500
		};

		context.ExceptionHandled = true;
	}
}

// Filtre API Identity: ValidateModelFilter
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DotnetNiger.Identity.Api.Filters;

// Validation automatique des ModelState.
public class ValidateModelFilter : IActionFilter
{
	public void OnActionExecuting(ActionExecutingContext context)
	{
		if (!context.ModelState.IsValid)
		{
			context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
		}
	}

	public void OnActionExecuted(ActionExecutedContext context)
	{
	}
}

// Validation Identity: LoginRequestValidator
using System.ComponentModel.DataAnnotations;
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.Exceptions;

namespace DotnetNiger.Identity.Application.Validators;

// Validation specifique pour login.
public static class LoginRequestValidator
{
	public static void ValidateAndThrow(LoginRequest request)
	{
		if (request == null)
		{
			throw new IdentityException("Login request is required.", 400);
		}

	if (string.IsNullOrWhiteSpace(request.Email))
		{
			throw new IdentityException("Email is required.", 400);
		}

		if (!new EmailAddressAttribute().IsValid(request.Email))
		{
			throw new IdentityException("Email is invalid.", 400);
		}

		if (string.IsNullOrWhiteSpace(request.Password))
		{
			throw new IdentityException("Password is required.", 400);
		}
	}
}

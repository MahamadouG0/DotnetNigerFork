// Validation Identity: RegisterRequestValidator
using System.ComponentModel.DataAnnotations;
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.Exceptions;

namespace DotnetNiger.Identity.Application.Validators;

// Validation specifique pour inscription.
public static class RegisterRequestValidator
{
	public static void ValidateAndThrow(RegisterRequest request)
	{
		if (request == null)
		{
			throw new IdentityException("Register request is required.", 400);
		}

		if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
		{
			throw new IdentityException("Username must be at least 3 characters.", 400);
		}

		if (!new EmailAddressAttribute().IsValid(request.Email))
		{
			throw new IdentityException("Email is invalid.", 400);
		}

		if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
		{
			throw new IdentityException("Password must be at least 8 characters.", 400);
		}

		if (string.IsNullOrWhiteSpace(request.FullName))
		{
			throw new IdentityException("Full name is required.", 400);
		}
	}
}

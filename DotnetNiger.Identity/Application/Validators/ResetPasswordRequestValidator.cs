// Validation Identity: ResetPasswordRequestValidator
using System.ComponentModel.DataAnnotations;
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.Exceptions;

namespace DotnetNiger.Identity.Application.Validators;

// Validation specifique pour reset password.
public static class ResetPasswordRequestValidator
{
	public static void ValidateAndThrow(ResetPasswordRequest request)
	{
		if (request == null)
		{
			throw new IdentityException("Reset password request is required.", 400);
		}

		if (!new EmailAddressAttribute().IsValid(request.Email))
		{
			throw new IdentityException("Email is invalid.", 400);
		}

		if (string.IsNullOrWhiteSpace(request.Token))
		{
			throw new IdentityException("Token is required.", 400);
		}

		if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
		{
			throw new IdentityException("New password must be at least 8 characters.", 400);
		}
	}
}

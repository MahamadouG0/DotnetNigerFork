// Validation Identity: UpdateProfileRequestValidator
using System.ComponentModel.DataAnnotations;
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.Exceptions;

namespace DotnetNiger.Identity.Application.Validators;

// Validation specifique pour la mise a jour du profil.
public static class UpdateProfileRequestValidator
{
	public static void ValidateAndThrow(UpdateProfileRequest request)
	{
		if (request == null)
		{
			throw new IdentityException("Profile update request is required.", 400);
		}

		if (request.FullName.Length > 100)
		{
			throw new IdentityException("Full name is too long.", 400);
		}

		if (request.Bio.Length > 500)
		{
			throw new IdentityException("Bio is too long.", 400);
		}

		if (!string.IsNullOrWhiteSpace(request.AvatarUrl) && !new UrlAttribute().IsValid(request.AvatarUrl))
		{
			throw new IdentityException("AvatarUrl is invalid.", 400);
		}
	}
}

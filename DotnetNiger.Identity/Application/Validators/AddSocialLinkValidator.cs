// Validation Identity: AddSocialLinkValidator
using System.ComponentModel.DataAnnotations;
using DotnetNiger.Identity.Application.DTOs.Requests;
using DotnetNiger.Identity.Application.Exceptions;

namespace DotnetNiger.Identity.Application.Validators;

// Validation specifique pour les liens sociaux.
public static class AddSocialLinkValidator
{
	private static readonly HashSet<string> AllowedPlatforms =
		new(StringComparer.OrdinalIgnoreCase) { "Twitter", "LinkedIn", "GitHub", "Facebook" };

	public static void ValidateAndThrow(AddSocialLinkRequest request)
	{
		if (request == null)
		{
			throw new IdentityException("Social link request is required.", 400);
		}

		if (string.IsNullOrWhiteSpace(request.Platform) || !AllowedPlatforms.Contains(request.Platform))
		{
			throw new IdentityException("Platform is invalid.", 400);
		}

		if (!new UrlAttribute().IsValid(request.Url))
		{
			throw new IdentityException("Url is invalid.", 400);
		}
	}
}

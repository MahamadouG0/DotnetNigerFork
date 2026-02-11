// Composant securite Identity: PasswordHasher
using DotnetNiger.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace DotnetNiger.Identity.Infrastructure.Security;

// Hash et verification pour mot de passe.
public class PasswordHasher
{
	private readonly IPasswordHasher<ApplicationUser> _hasher;

	public PasswordHasher(IPasswordHasher<ApplicationUser> hasher)
	{
		_hasher = hasher;
	}

	public string Hash(ApplicationUser user, string password)
	{
		return _hasher.HashPassword(user, password);
	}

	public bool Verify(ApplicationUser user, string hashedPassword, string password)
	{
		var result = _hasher.VerifyHashedPassword(user, hashedPassword, password);
		return result == PasswordVerificationResult.Success;
	}
}

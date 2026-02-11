// Exception metier Identity: UserNotFoundException
namespace DotnetNiger.Identity.Application.Exceptions;

public class UserNotFoundException : IdentityException
{
	// Utilisateur introuvable.
	public UserNotFoundException()
		: base("User not found.", 404)
	{
	}
}

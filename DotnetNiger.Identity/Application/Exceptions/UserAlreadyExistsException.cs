// Exception metier Identity: UserAlreadyExistsException
namespace DotnetNiger.Identity.Application.Exceptions;

public class UserAlreadyExistsException : IdentityException
{
	// Utilisateur deja existant.
	public UserAlreadyExistsException(string message)
		: base(message, 409)
	{
	}
}

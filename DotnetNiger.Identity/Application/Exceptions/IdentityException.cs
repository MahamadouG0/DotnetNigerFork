// Exception metier Identity: IdentityException
namespace DotnetNiger.Identity.Application.Exceptions;

public class IdentityException : Exception
{
	// Exception metier avec code HTTP.
	public IdentityException(string message, int statusCode)
		: base(message)
	{
		StatusCode = statusCode;
	}

	public int StatusCode { get; }
}

namespace DotnetNiger.Identity.Application.Exceptions;

public class EmailAlreadyExistsException : Exception
{
    public string Email { get; }
    public EmailAlreadyExistsException(string email)
        : base($"Un compte avec l'email '{email}' existe déjà")
    {
        Email = email;
    }
}

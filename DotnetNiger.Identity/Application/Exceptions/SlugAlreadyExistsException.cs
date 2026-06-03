namespace DotnetNiger.Identity.Application.Exceptions;

public class SlugAlreadyExistsException : Exception
{
    public string Slug { get; }
    public SlugAlreadyExistsException(string slug)
        : base($"Le slug '{slug}' est déjà utilisé par un autre tenant")
    {
        Slug = slug;
    }
}

// Integration externe Identity: IEmailProvider
namespace DotnetNiger.Identity.Infrastructure.External;

// Contrat pour un provider d'envoi d'email.
public interface IEmailProvider
{
	string Name { get; }
	Task SendAsync(string to, string subject, string body);
}

// Contrat applicatif Identity: IEmailService
namespace DotnetNiger.Identity.Application.Services.Interfaces;

// Contrat pour l'envoi d'emails.
public interface IEmailService
{
	Task SendAsync(string to, string subject, string body);
}

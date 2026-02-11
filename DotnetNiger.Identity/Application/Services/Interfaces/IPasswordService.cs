// Contrat applicatif Identity: IPasswordService
namespace DotnetNiger.Identity.Application.Services.Interfaces;

// Contrat pour utilitaires de mot de passe.
public interface IPasswordService
{
	string GenerateRandom(int length = 16);
	bool IsStrong(string password);
}

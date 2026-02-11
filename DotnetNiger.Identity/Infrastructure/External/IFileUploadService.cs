// Integration externe Identity: IFileUploadService
namespace DotnetNiger.Identity.Infrastructure.External;

// Contrat pour l'upload de fichiers.
public interface IFileUploadService
{
	Task<string> UploadAsync(Stream content, string fileName, string contentType);
	Task DeleteAsync(string fileUrl);
}

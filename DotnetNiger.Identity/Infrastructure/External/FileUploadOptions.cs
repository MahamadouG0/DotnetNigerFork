// Integration externe Identity: FileUploadOptions
namespace DotnetNiger.Identity.Infrastructure.External;

// Options de configuration pour l'upload de fichiers.
public class FileUploadOptions
{
	public string Provider { get; set; } = "Local";
	public string RootPath { get; set; } = "uploads";
	public string PublicBasePath { get; set; } = "/uploads";
	public long MaxAvatarBytes { get; set; } = 2_000_000;
	public string[] AllowedAvatarContentTypes { get; set; } =
	{
		"image/jpeg",
		"image/png",
		"image/webp"
	};
	public string[] AllowedAvatarExtensions { get; set; } =
	{
		".jpg",
		".jpeg",
		".png",
		".webp"
	};
	public bool CleanupEnabled { get; set; }
	public int CleanupIntervalMinutes { get; set; } = 1440;
	public int CleanupOrphanDays { get; set; } = 7;
	public FileUploadAzureOptions Azure { get; set; } = new();
}

public class FileUploadAzureOptions
{
	public string ConnectionString { get; set; } = string.Empty;
	public string Container { get; set; } = "dotnetniger-uploads";
	public string PublicBaseUrl { get; set; } = string.Empty;
}

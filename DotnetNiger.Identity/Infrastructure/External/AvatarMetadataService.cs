// Integration externe Identity: AvatarMetadataService
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using DotnetNiger.Identity.Application.DTOs.Responses;
using DotnetNiger.Identity.Application.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DotnetNiger.Identity.Infrastructure.External;

// Lecture des metadonnees d'avatar selon le provider configure.
public class AvatarMetadataService : IAvatarMetadataService
{
	private readonly FileUploadOptions _options;
	private readonly IWebHostEnvironment _environment;

	public AvatarMetadataService(IOptions<FileUploadOptions> options, IWebHostEnvironment environment)
	{
		_options = options.Value;
		_environment = environment;
	}

	public async Task<AvatarInfoDto> GetMetadataAsync(string? avatarUrl)
	{
		var hasAvatar = !string.IsNullOrWhiteSpace(avatarUrl);
		var result = new AvatarInfoDto
		{
			Url = avatarUrl ?? string.Empty,
			HasAvatar = hasAvatar,
			Provider = _options.Provider
		};

		if (!hasAvatar)
		{
			return result;
		}

		if (string.Equals(_options.Provider, "Azure", StringComparison.OrdinalIgnoreCase))
		{
			return await GetAzureMetadataAsync(result);
		}

		return GetLocalMetadata(result);
	}

	private AvatarInfoDto GetLocalMetadata(AvatarInfoDto result)
	{
		var relativePath = ExtractRelativePath(result.Url);
		if (string.IsNullOrWhiteSpace(relativePath))
		{
			return result;
		}

		var fullPath = Path.Combine(_environment.ContentRootPath, _options.RootPath, relativePath);
		if (!File.Exists(fullPath))
		{
			return result;
		}

		var info = new FileInfo(fullPath);
		result.Exists = true;
		result.SizeBytes = info.Length;
		result.FileName = info.Name;
		result.ContentType = GuessContentType(info.Extension);
		return result;
	}

	private async Task<AvatarInfoDto> GetAzureMetadataAsync(AvatarInfoDto result)
	{
		if (string.IsNullOrWhiteSpace(_options.Azure.ConnectionString) || string.IsNullOrWhiteSpace(_options.Azure.Container))
		{
			return result;
		}

		var blobName = ExtractBlobName(result.Url, out var containerName);
		if (string.IsNullOrWhiteSpace(blobName))
		{
			return result;
		}

		var container = string.IsNullOrWhiteSpace(containerName) ? _options.Azure.Container : containerName;
		var containerClient = new BlobContainerClient(_options.Azure.ConnectionString, container);
		var blobClient = containerClient.GetBlobClient(blobName);
		try
		{
			var properties = await blobClient.GetPropertiesAsync();
			result.Exists = true;
			result.SizeBytes = properties.Value.ContentLength;
			result.ContentType = properties.Value.ContentType ?? string.Empty;
			result.FileName = Path.GetFileName(blobName);
			return result;
		}
		catch
		{
			return result;
		}
	}

	private string ExtractRelativePath(string fileUrl)
	{
		var basePath = NormalizePublicBasePath(_options.PublicBasePath);
		var rawPath = fileUrl;
		if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
		{
			rawPath = uri.AbsolutePath;
		}

		if (!rawPath.StartsWith('/'))
		{
			rawPath = "/" + rawPath;
		}

		if (!rawPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
		{
			return string.Empty;
		}

		return rawPath[basePath.Length..].TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
	}

	private string ExtractBlobName(string fileUrl, out string? containerName)
	{
		containerName = null;
		var publicBaseUrl = _options.Azure.PublicBaseUrl;
		if (!string.IsNullOrWhiteSpace(publicBaseUrl) &&
			fileUrl.StartsWith(publicBaseUrl, StringComparison.OrdinalIgnoreCase))
		{
			return fileUrl[publicBaseUrl.TrimEnd('/').Length..].TrimStart('/');
		}

		if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
		{
			var builder = new BlobUriBuilder(uri);
			containerName = builder.BlobContainerName;
			return builder.BlobName;
		}

		return fileUrl.TrimStart('/');
	}

	private static string NormalizePublicBasePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "/uploads";
		}

		var trimmed = path.Trim();
		if (!trimmed.StartsWith('/'))
		{
			trimmed = "/" + trimmed;
		}

		return trimmed.TrimEnd('/');
	}

	private static string GuessContentType(string extension)
	{
		return extension.ToLowerInvariant() switch
		{
			".jpg" => "image/jpeg",
			".jpeg" => "image/jpeg",
			".png" => "image/png",
			".webp" => "image/webp",
			_ => string.Empty
		};
	}
}

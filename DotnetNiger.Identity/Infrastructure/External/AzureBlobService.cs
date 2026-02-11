// Integration externe Identity: AzureBlobService
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace DotnetNiger.Identity.Infrastructure.External;

// Service d'upload de fichiers via Azure Blob Storage.
public class AzureBlobService : IFileUploadService
{
	private readonly FileUploadOptions _options;
	private readonly BlobContainerClient _containerClient;

	public AzureBlobService(IOptions<FileUploadOptions> options)
	{
		_options = options.Value;
		var azure = _options.Azure;
		if (string.IsNullOrWhiteSpace(azure.ConnectionString) || string.IsNullOrWhiteSpace(azure.Container))
		{
			throw new InvalidOperationException("Azure Blob Storage is not configured.");
		}

		_containerClient = new BlobContainerClient(azure.ConnectionString, azure.Container);
		_containerClient.CreateIfNotExists(PublicAccessType.Blob);
	}

	public async Task<string> UploadAsync(Stream content, string fileName, string contentType)
	{
		if (content == null)
		{
			throw new ArgumentNullException(nameof(content));
		}

		if (string.IsNullOrWhiteSpace(fileName))
		{
			throw new ArgumentException("File name is required.", nameof(fileName));
		}

		var blobName = SanitizeBlobName(fileName);
		var blobClient = _containerClient.GetBlobClient(blobName);
		var uploadOptions = new BlobUploadOptions
		{
			HttpHeaders = new BlobHttpHeaders
			{
				ContentType = contentType
			}
		};

		await blobClient.UploadAsync(content, uploadOptions);

		var publicBaseUrl = _options.Azure.PublicBaseUrl;
		if (!string.IsNullOrWhiteSpace(publicBaseUrl))
		{
			return $"{publicBaseUrl.TrimEnd('/')}/{blobName}";
		}

		return blobClient.Uri.ToString();
	}

	public async Task DeleteAsync(string fileUrl)
	{
		if (string.IsNullOrWhiteSpace(fileUrl))
		{
			return;
		}

		var blobName = ExtractBlobName(fileUrl, out var containerName);
		if (string.IsNullOrWhiteSpace(blobName))
		{
			return;
		}

		var container = string.IsNullOrWhiteSpace(containerName) ? _options.Azure.Container : containerName;
		var containerClient = container.Equals(_options.Azure.Container, StringComparison.OrdinalIgnoreCase)
			? _containerClient
			: new BlobContainerClient(_options.Azure.ConnectionString, container);

		var blobClient = containerClient.GetBlobClient(blobName);
		await blobClient.DeleteIfExistsAsync();
	}

	private static string SanitizeBlobName(string fileName)
	{
		var normalized = fileName.Replace('\\', '/').TrimStart('/');
		if (normalized.Contains("..", StringComparison.Ordinal))
		{
			throw new InvalidOperationException("Invalid file name.");
		}

		var segments = normalized
			.Split('/', StringSplitOptions.RemoveEmptyEntries)
			.Select(Path.GetFileName)
			.Where(segment => !string.IsNullOrWhiteSpace(segment))
			.ToArray();

		if (segments.Length == 0)
		{
			throw new InvalidOperationException("Invalid file name.");
		}

		return string.Join('/', segments);
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
}

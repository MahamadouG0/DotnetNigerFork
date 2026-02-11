// Integration externe Identity: FileSystemUploadService
using Microsoft.Extensions.Options;

namespace DotnetNiger.Identity.Infrastructure.External;

// Service d'upload de fichiers sur le disque local.
public class FileSystemUploadService : IFileUploadService
{
	private readonly FileUploadOptions _options;
	private readonly IWebHostEnvironment _environment;

	public FileSystemUploadService(IOptions<FileUploadOptions> options, IWebHostEnvironment environment)
	{
		_options = options.Value;
		_environment = environment;
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

		var relativePath = SanitizeRelativePath(fileName);
		var rootPath = Path.Combine(_environment.ContentRootPath, _options.RootPath);
		var fullPath = Path.Combine(rootPath, relativePath);
		var directory = Path.GetDirectoryName(fullPath);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}

		await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
		await content.CopyToAsync(fileStream);

		var basePath = NormalizePublicBasePath(_options.PublicBasePath);
		var publicPath = $"{basePath}/{relativePath.Replace('\\', '/')}";
		return publicPath;
	}

	public Task DeleteAsync(string fileUrl)
	{
		if (string.IsNullOrWhiteSpace(fileUrl))
		{
			return Task.CompletedTask;
		}

		var publicPath = ExtractRelativePath(fileUrl);
		if (string.IsNullOrWhiteSpace(publicPath))
		{
			return Task.CompletedTask;
		}

		var rootPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, _options.RootPath));
		var fullPath = Path.GetFullPath(Path.Combine(rootPath, publicPath));
		if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Invalid file path.");
		}

		if (File.Exists(fullPath))
		{
			File.Delete(fullPath);
		}

		return Task.CompletedTask;
	}

	private static string SanitizeRelativePath(string fileName)
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

		return Path.Combine(segments);
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

		var relative = rawPath[basePath.Length..].TrimStart('/');
		return relative.Replace('/', Path.DirectorySeparatorChar);
	}
}

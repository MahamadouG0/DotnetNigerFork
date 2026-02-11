// Extension API Identity: ServiceExtensions
using DotnetNiger.Identity.Application.Services;
using DotnetNiger.Identity.Application.Services.Interfaces;
using DotnetNiger.Identity.Infrastructure.Caching;
using DotnetNiger.Identity.Infrastructure.External;
using DotnetNiger.Identity.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetNiger.Identity.Api.Extensions;

// Extensions pour l'enregistrement des services.
public static class ServiceExtensions
{
	public static IServiceCollection AddIdentityApplicationServices(this IServiceCollection services)
	{
		services.AddScoped<IAuthService, AuthService>();
		services.AddScoped<IUserService, UserService>();
		services.AddScoped<ITokenService, TokenService>();
		services.AddScoped<IEmailService, EmailService>();
		services.AddScoped<ISocialLinkService, SocialLinkService>();
		services.AddScoped<IRoleService, RoleService>();
		services.AddScoped<IPermissionService, PermissionService>();
		services.AddScoped<IAdminService, AdminService>();
		services.AddScoped<IApiKeyService, ApiKeyService>();
		services.AddScoped<ILoginHistoryService, LoginHistoryService>();
		services.AddScoped<IPasswordService, PasswordService>();
		services.AddScoped<ICacheService, RedisCacheService>();
		services.AddScoped<IAvatarMetadataService, AvatarMetadataService>();
		services.AddScoped<FileSystemUploadService>();
		services.AddScoped<AzureBlobService>();
		services.AddScoped<IFileUploadService>(serviceProvider =>
		{
			var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileUploadOptions>>().Value;
			var provider = options.Provider?.Trim() ?? "";
			return provider.Equals("Azure", StringComparison.OrdinalIgnoreCase)
				? serviceProvider.GetRequiredService<AzureBlobService>()
				: serviceProvider.GetRequiredService<FileSystemUploadService>();
		});
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
		services.AddScoped<ILoginHistoryRepository, LoginHistoryRepository>();
		return services;
	}

	public static IServiceCollection AddEmailProviders(this IServiceCollection services)
	{
		services.AddScoped<IEmailProvider, SmtpEmailProvider>();
		services.AddScoped<IEmailProvider, SendGridProvider>();
		services.AddScoped<IEmailProvider, MailgunProvider>();
		return services;
	}
}

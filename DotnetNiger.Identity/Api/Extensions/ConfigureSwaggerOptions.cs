// Extension API Identity: ConfigureSwaggerOptions
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DotnetNiger.Identity.Api.Extensions;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
	// Configuration Swagger par version d'API.
	private readonly IApiVersionDescriptionProvider _provider;

	public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
	{
		_provider = provider;
	}

	public void Configure(SwaggerGenOptions options)
	{
		foreach (var description in _provider.ApiVersionDescriptions)
		{
			options.SwaggerDoc(description.GroupName, new OpenApiInfo
			{
				Title = "Identity Service API",
				Version = description.ApiVersion.ToString(),
				Description = "API pour l'authentification et la gestion des identites de DotnetNiger"
			});
		}

		// Support du bouton Authorize pour JWT.
		options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
		{
			Description = "Collez uniquement le token JWT (sans 'Bearer').",
			Name = "Authorization",
			In = ParameterLocation.Header,
			Type = SecuritySchemeType.Http,
			Scheme = "bearer",
			BearerFormat = "JWT"
		});

		options.AddSecurityRequirement(new OpenApiSecurityRequirement
		{
			{
				new OpenApiSecurityScheme
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.SecurityScheme,
						Id = "Bearer"
					}
				},
				Array.Empty<string>()
			}
		});
	}
}

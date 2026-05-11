using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenIddict.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using Asp.Versioning;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using DotnetNiger.Identity.Application.Services;

namespace DotnetNiger.Identity.Api;

public static class ServiceExtensions
{
    /// <summary>Configure l'infrastructure : DbContext, Identity, OpenIddict, Auth externes, CORS.</summary>
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseSqlite(config.GetConnectionString("DefaultConnection"));
            options.UseOpenIddict();
        });

        services.Configure<SmtpOptions>(config.GetSection("Smtp"));

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        services.AddOpenIddict()
            .AddCore(core => core.UseEntityFrameworkCore().UseDbContext<IdentityDbContext>())
            .AddServer(server =>
            {
                server.SetTokenEndpointUris("/connect/token")
                      .SetUserinfoEndpointUris("/connect/userinfo");

                server.AllowPasswordFlow()
                      .AllowRefreshTokenFlow()
                      .SetRefreshTokenLifetime(TimeSpan.FromDays(14))
                      .SetRefreshTokenReuseLeeway(TimeSpan.FromSeconds(30))
                      .AcceptAnonymousClients();

                server.AddEphemeralEncryptionKey()
                      .AddEphemeralSigningKey();

                server.UseAspNetCore()
                      .EnableTokenEndpointPassthrough()
                      .DisableTransportSecurityRequirement();

                server.RegisterScopes(
                    OpenIddict.Abstractions.OpenIddictConstants.Scopes.OpenId,
                    OpenIddict.Abstractions.OpenIddictConstants.Scopes.Email,
                    OpenIddict.Abstractions.OpenIddictConstants.Scopes.Profile,
                    OpenIddict.Abstractions.OpenIddictConstants.Scopes.Roles,
                    OpenIddict.Abstractions.OpenIddictConstants.Scopes.OfflineAccess,
                    "api");
            })
            .AddValidation(validation =>
            {
                validation.UseLocalServer();
                validation.UseAspNetCore();
            });

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        var googleId = config["Authentication:Google:ClientId"];
        if (!string.IsNullOrEmpty(googleId))
        {
            authBuilder.AddGoogle(google =>
            {
                google.ClientId = googleId;
                google.ClientSecret = config["Authentication:Google:ClientSecret"] ?? "";
            });
        }

        var msId = config["Authentication:Microsoft:ClientId"];
        if (!string.IsNullOrEmpty(msId))
        {
            authBuilder.AddMicrosoftAccount(microsoft =>
            {
                microsoft.ClientId = msId;
                microsoft.ClientSecret = config["Authentication:Microsoft:ClientSecret"] ?? "";
            });
        }

        var ghId = config["Authentication:GitHub:ClientId"];
        if (!string.IsNullOrEmpty(ghId))
        {
            authBuilder.AddOAuth("GitHub", github =>
            {
                github.ClientId = ghId;
                github.ClientSecret = config["Authentication:GitHub:ClientSecret"] ?? "";
                github.CallbackPath = "/signin-github";
                github.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                github.TokenEndpoint = "https://github.com/login/oauth/access_token";
                github.UserInformationEndpoint = "https://api.github.com/user";
                github.Scope.Add("user:email");

                github.Events.OnCreatingTicket = async ctx =>
                {
                    if (ctx.Identity == null || ctx.AccessToken == null) return;

                    var userElement = ctx.User;
                    var userId = userElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                    var login = userElement.TryGetProperty("login", out var lEl) ? lEl.GetString() : null;
                    var name = userElement.TryGetProperty("name", out var nEl) ? nEl.GetString() : null;
                    var email = userElement.TryGetProperty("email", out var eEl) ? eEl.GetString() : null;

                    if (userId != null)
                    {
                        ctx.Identity.AddClaim(new("sub", userId));
                        ctx.Identity.AddClaim(new(
                            System.Security.Claims.ClaimTypes.NameIdentifier, userId));
                    }
                    if (login != null) ctx.Identity.AddClaim(new(
                        System.Security.Claims.ClaimTypes.Name, name ?? login));
                    if (email != null) ctx.Identity.AddClaim(new(
                        System.Security.Claims.ClaimTypes.Email, email));
                    else
                    {
                        var req = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                        req.Headers.Authorization = new("Bearer", ctx.AccessToken);
                        req.Headers.UserAgent.Add(new("DotnetNiger", "1.0"));
                        using var resp = await ctx.Backchannel.SendAsync(req);
                        var emails = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(
                            await resp.Content.ReadAsStringAsync());
                        foreach (var item in emails ?? [])
                            if (item.TryGetProperty("primary", out var p) && p.GetBoolean()
                                && item.TryGetProperty("email", out var ev))
                            {
                                ctx.Identity.AddClaim(new(
                                    System.Security.Claims.ClaimTypes.Email, ev.GetString()!));
                                break;
                            }
                    }
                };
            });
        }

        services.AddCors(options => options.AddPolicy("GatewayOnly", builder =>
            builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

        return services;
    }

    /// <summary>Enregistre les services métier (scoped).</summary>
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddScoped<TenantContext>();
        services.AddScoped<TenantResolutionService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<RoleService>();
        services.AddScoped<PermissionService>();
        services.AddScoped<TenantService>();
        services.AddScoped<AdminService>();
        services.AddScoped<IEmailSender<ApplicationUser>, EmailSender>();

        return services;
    }

    /// <summary>Configure API Versioning (recommandation Microsoft : URL segment + rapport version).</summary>
    public static IServiceCollection AddApiVersioningWithSwagger(
        this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DotnetNiger Identity API",
                Version = "v1.0",
                Description = "Identity Provider multi-tenant avec OpenIddict (OAuth2/OIDC).\n\n" +
                              "**Auth locale** : `POST /api/v1/auth/login` puis `POST /connect/token`\n" +
                              "**Auth externe** : `GET /api/v1/auth/external-login?provider=Google|GitHub|Microsoft`\n" +
                              "**Inscription** : `POST /api/v1/auth/register` + confirmation email\n" +
                              "**Health** : `GET /health` ou `GET /api/v1/diagnostics/health`\n\n" +
                              "### Obtenir un JWT\n" +
                              "```\n" +
                              "curl -X POST http://localhost:5075/connect/token \\\n" +
                              "  -H \"Content-Type: application/x-www-form-urlencoded\" \\\n" +
                              "  -d \"grant_type=password&username=admin@dotnetniger.com&password=Admin%40123456&scope=openid+profile+email+roles+offline_access&remember_me=true\"\n" +
                              "```"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                Description = "Entrez le token JWT : Bearer {votre_token}"
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

            // Active les XML comments dans Swagger
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });

        return services;
    }
}

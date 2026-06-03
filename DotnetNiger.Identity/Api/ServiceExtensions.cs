using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenIddict.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using Asp.Versioning;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;
using DotnetNiger.Identity.Application.Services;
using DotnetNiger.Identity.Api.Authentication;

namespace DotnetNiger.Identity.Api;

public static class ServiceExtensions
{
    /// <summary>Configure l'infrastructure : DbContext, Identity, OpenIddict, Auth externes, CORS.</summary>
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        services.AddMemoryCache();
        services.AddHostedService<GdprCleanupService>();

        services.AddDbContext<IdentityDbContext>(options =>
        {
            var provider = config.GetValue<string>("DatabaseProvider", "Sqlite");
            var connStr = config.GetConnectionString("DefaultConnection") ?? "Data Source=DotnetNigerIdentity.db";

            if (provider == "SqlServer")
                options.UseSqlServer(connStr, x => x.MigrationsAssembly("DotnetNiger.Identity"));
            else if (provider is "PostgreSql" or "PostgreSQL" or "Npgsql")
                options.UseNpgsql(connStr, x => x.MigrationsAssembly("DotnetNiger.Identity"));
            else
                options.UseSqlite(connStr, x => x.MigrationsAssembly("DotnetNiger.Identity"));

            options.UseOpenIddict();
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.Configure<SmtpOptions>(config.GetSection("Smtp"));

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(1);
        });

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
                      .SetAuthorizationEndpointUris("/connect/authorize")
                      .SetLogoutEndpointUris("/connect/logout")
                      .SetUserinfoEndpointUris("/connect/userinfo");

                server.AllowPasswordFlow()
                      .AllowRefreshTokenFlow()
                      .AllowAuthorizationCodeFlow()
                          .RequireProofKeyForCodeExchange()
                      .AllowClientCredentialsFlow()
                      .SetRefreshTokenLifetime(TimeSpan.FromDays(14))
                      .SetRefreshTokenReuseLeeway(TimeSpan.FromSeconds(0));

                server.DisableAccessTokenEncryption();

                if (env.IsDevelopment())
                {
                    // Load development certificate for HTTPS
                    var certPath = Path.Combine(AppContext.BaseDirectory, "https", "localhost.pfx");
                    var certPassword = "1234"; // Default password for dotnet dev certs
                    
                    if (File.Exists(certPath))
                    {
                        try
                        {
                            using var cert = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12(
                                File.ReadAllBytes(certPath), certPassword);
                            server.AddEncryptionCertificate(cert)
                                  .AddSigningCertificate(cert);
                        }
                        catch
                        {
                            server.AddEphemeralEncryptionKey()
                                  .AddEphemeralSigningKey();
                        }
                    }
                    else
                    {
                        server.AddEphemeralEncryptionKey()
                              .AddEphemeralSigningKey();
                    }
                    
                    server.IgnoreEndpointPermissions()
                          .IgnoreGrantTypePermissions()
                          .IgnoreScopePermissions();
                    server.AcceptAnonymousClients();
                }
                else
                {
                    var certPath = config["OpenIddict:CertificatePath"] ?? "/etc/ssl/certs/opendict.pfx";
                    var certPassword = config["OpenIddict:CertificatePassword"] ?? "";
                    if (File.Exists(certPath))
                    {
                        using var cert = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12(File.ReadAllBytes(certPath), certPassword);
                        server.AddEncryptionCertificate(cert)
                              .AddSigningCertificate(cert);
                    }
                    else
                    {
                        server.AddEphemeralEncryptionKey()
                              .AddEphemeralSigningKey();
                    }
                }

                var aspNetCore = server.UseAspNetCore()
                      .EnableTokenEndpointPassthrough()
                      .EnableAuthorizationEndpointPassthrough()
                      .EnableLogoutEndpointPassthrough();

                if (env.IsDevelopment())
                    aspNetCore.DisableTransportSecurityRequirement();

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
        })
        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationDefaults.AuthenticationScheme, null);

        var googleId = config["Authentication:Google:ClientId"];
        if (!string.IsNullOrEmpty(googleId))
        {
            authBuilder.AddGoogle(google =>
            {
                google.ClientId = googleId;
                google.ClientSecret = config["Authentication:Google:ClientSecret"] ?? "";
                google.SignInScheme = IdentityConstants.ExternalScheme;
                google.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                google.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                google.Events.OnRemoteFailure = ctx =>
                {
                    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ctx.Failure, "Google OAuth remote failure: {Message} | Inner: {Inner} | Stack: {Stack}",
                        ctx.Failure?.Message, ctx.Failure?.InnerException?.Message, ctx.Failure?.StackTrace);
                    ctx.Response.Redirect($"/Account/Login?error={Uri.EscapeDataString(ctx.Failure?.Message ?? "google_failed")}");
                    ctx.HandleResponse();
                    return Task.CompletedTask;
                };
            });
        }

        var ghId = config["Authentication:GitHub:ClientId"];
        if (!string.IsNullOrEmpty(ghId))
        {
            authBuilder.AddOAuth("GitHub", "GitHub", github =>
            {
                github.ClientId = ghId;
                github.ClientSecret = config["Authentication:GitHub:ClientSecret"] ?? "";
                github.SignInScheme = IdentityConstants.ExternalScheme;
                github.CallbackPath = "/signin-github";
                github.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                github.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                github.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                github.TokenEndpoint = "https://github.com/login/oauth/access_token";
                github.UserInformationEndpoint = "https://api.github.com/user";
                github.Scope.Add("user:email");

                github.Events.OnRemoteFailure = ctx =>
                {
                    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ctx.Failure, "GitHub OAuth remote failure: {Message} | Inner: {Inner} | Stack: {Stack}",
                        ctx.Failure?.Message, ctx.Failure?.InnerException?.Message, ctx.Failure?.StackTrace);
                    ctx.Response.Redirect($"/Account/Login?error={Uri.EscapeDataString(ctx.Failure?.Message ?? "github_failed")}");
                    ctx.HandleResponse();
                    return Task.CompletedTask;
                };
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

        return services;
    }

    /// <summary>Configure CORS en fonction de l'environnement.</summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            if (environment.IsDevelopment())
                options.AddPolicy("AllowAll", builder =>
                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            else
            {
                var origins = configuration["Cors:AllowedOrigins"];
                if (!string.IsNullOrWhiteSpace(origins))
                    options.AddPolicy("AllowAll", builder =>
                        builder.WithOrigins(origins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                              .AllowAnyMethod().AllowAnyHeader().AllowCredentials());
                else
                    options.AddPolicy("AllowAll", builder =>
                        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            }
        });

        return services;
    }

    /// <summary>Configure le rate limiting pour les endpoints publics.</summary>
    public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services, IConfiguration config)
    {
        var permitLimit = int.TryParse(config["RateLimiting:PermitLimit"], out var p) ? p : 5;
        var windowSeconds = int.TryParse(config["RateLimiting:WindowSeconds"], out var w) ? w : 60;
        var authPermitLimit = int.TryParse(config["RateLimiting:AuthPermitLimit"], out var ap) ? ap : 20;
        var authWindowSeconds = int.TryParse(config["RateLimiting:AuthWindowSeconds"], out var aw) ? aw : 60;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("TenantRegistration", opt =>
            {
                opt.PermitLimit = permitLimit;
                opt.Window = TimeSpan.FromSeconds(windowSeconds);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("Auth", opt =>
            {
                opt.PermitLimit = authPermitLimit;
                opt.Window = TimeSpan.FromSeconds(authWindowSeconds);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 500,
                        Window = TimeSpan.FromSeconds(60),
                        QueueLimit = 0
                    }));
        });

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
        services.AddScoped<TenantClientService>();
        services.AddScoped<TenantApiKeyService>();
        services.AddScoped<AdminService>();
        services.AddScoped<IEmailSender<ApplicationUser>, EmailSender>();
        services.AddScoped<ExternalServiceService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<GdprService>();

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

using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Abstractions;
using DotnetNiger.Identity.Api;
using DotnetNiger.Identity.Infrastructure;

namespace DotnetNiger.Identity.IntegrationTests;

public class IdentityWebApplicationFactory : IAsyncLifetime
{
    private WebApplication _app = null!;
    private readonly SqliteConnection _connection;
    private readonly IConfigurationRoot _testConfig;

    public IdentityWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _testConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "DotnetNiger.Identity",
                ["Jwt:Audience"] = "DotnetNiger.Identity.Client",
                ["Jwt:Key"] = "test-secret-key-1234567890-extra-padding-for-32!",
                ["Smtp:Host"] = "",
                ["InternalApiKey"] = "test-internal-key",
                ["DatabaseProvider"] = "Sqlite",
                ["Admin:DefaultPassword"] = "Admin@123456"
            })
            .Build();
    }

    public HttpClient HttpClient { get; private set; } = null!;

    public HttpClient CreateClient() => HttpClient;

    public async Task InitializeAsync()
    {
        var builder = ApplicationSetup.CreateBuilder([]);
        builder.Configuration.AddConfiguration(_testConfig);

        builder.Services.RemoveAll<DbContextOptions<IdentityDbContext>>();
        builder.Services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseSqlite(_connection);
            options.UseOpenIddict();
        });

        var app = ApplicationSetup.ConfigureApp(builder);

        using var seedScope = app.Services.CreateScope();
        var db = seedScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        db.Database.Migrate();

        var tenantContext = seedScope.ServiceProvider.GetRequiredService<TenantContext>();
        var userManager = seedScope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Domain.Entities.ApplicationUser>>();
        var roleManager = seedScope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Domain.Entities.ApplicationRole>>();
        var appManager = seedScope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        await DbSeeder.SeedAsync(db, userManager, roleManager, tenantContext, "Admin@123456", appManager);

        var testClient = await appManager.FindByClientIdAsync("test-client");
        if (testClient == null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "test-client",
                DisplayName = "Test Client",
                ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
                ClientType = OpenIddictConstants.ClientTypes.Public,
                ApplicationType = OpenIddictConstants.ApplicationTypes.Web
            };
            descriptor.Permissions.Add("ep:token");
            descriptor.Permissions.Add("gt:password");
            descriptor.Permissions.Add("gt:refresh_token");
            descriptor.Permissions.Add("scp:openid");
            descriptor.Permissions.Add("scp:email");
            descriptor.Permissions.Add("scp:profile");
            descriptor.Permissions.Add("scp:roles");
            descriptor.Permissions.Add("scp:offline_access");
            await appManager.CreateAsync(descriptor);
        }

        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        _app = app;
        var address = app.Urls.First();
        Console.Error.WriteLine($"Test app URL: {address}");
        HttpClient = new HttpClient { BaseAddress = new Uri(address) };
    }

    public async Task DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        _connection.Dispose();
    }
}

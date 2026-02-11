using DotnetNiger.Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotnetNiger.Identity.IntegrationTests;

public class IdentityWebApplicationFactory : WebApplicationFactory<Program>
{
	private SqliteConnection? _connection;

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.ConfigureAppConfiguration(config =>
		{
			var settings = new Dictionary<string, string?>
			{
				["Jwt:Issuer"] = "DotnetNiger.Identity",
				["Jwt:Audience"] = "DotnetNiger.Identity.Client",
				["Jwt:Key"] = "test-secret-key-1234567890",
				["Email:Enabled"] = "false",
				["FileUpload:CleanupEnabled"] = "false"
			};

			config.AddInMemoryCollection(settings);
		});

		builder.ConfigureServices(services =>
		{
			services.RemoveAll<DbContextOptions<DotnetNigerIdentityDbContext>>();

			_connection = new SqliteConnection("DataSource=:memory:");
			_connection.Open();

			services.AddDbContext<DotnetNigerIdentityDbContext>(options =>
				options.UseSqlite(_connection));

			using var scope = services.BuildServiceProvider().CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<DotnetNigerIdentityDbContext>();
			db.Database.EnsureCreated();
		});
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing)
		{
			_connection?.Dispose();
			_connection = null;
		}
	}
}

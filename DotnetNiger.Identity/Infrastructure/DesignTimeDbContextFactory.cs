using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using DotnetNiger.Identity.Domain.Entities;

namespace DotnetNiger.Identity.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var provider = config.GetValue<string>("DatabaseProvider", "Sqlite");
        var connStr = config.GetConnectionString("DefaultConnection") ?? "Data Source=DotnetNigerIdentity.db";

        var options = new DbContextOptionsBuilder<IdentityDbContext>();

        if (provider == "SqlServer")
            options.UseSqlServer(connStr, x => x.MigrationsAssembly("DotnetNiger.Identity"));
        else if (provider is "PostgreSql" or "PostgreSQL" or "Npgsql")
            options.UseNpgsql(connStr, x => x.MigrationsAssembly("DotnetNiger.Identity"));
        else
            options.UseSqlite(connStr, x => x.MigrationsAssembly("DotnetNiger.Identity"));

        options.UseOpenIddict();

        return new IdentityDbContext(options.Options, new TenantContext());
    }
}

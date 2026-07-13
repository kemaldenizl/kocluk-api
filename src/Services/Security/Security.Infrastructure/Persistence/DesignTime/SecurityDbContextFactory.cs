using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Security.Infrastructure.Tenancy;

namespace Security.Infrastructure.Persistence.DesignTime;

public sealed class SecurityDbContextFactory : IDesignTimeDbContextFactory<SecurityDbContext>
{
    public SecurityDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Connection string 'Postgres' was not found.");

        var builder = new DbContextOptionsBuilder<SecurityDbContext>();
        builder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsHistoryTable("__ef_migrations_history", "security");
        });

        builder.ConfigureWarnings(warnings =>
            warnings.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));

        var tenantContext = new TenantContext(Options.Create(new MultiTenancyOptions()));

        return new SecurityDbContext(builder.Options, tenantContext);
    }
}
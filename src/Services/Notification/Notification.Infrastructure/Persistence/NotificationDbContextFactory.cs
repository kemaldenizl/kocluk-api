using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Notification.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build the context without spinning up the host. Uses the
/// <c>ConnectionStrings__Postgres</c> environment variable when present, otherwise a local dev fallback.
/// Migration generation does not connect to the database, so the fallback is sufficient.
/// </summary>
public sealed class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
                               ?? "Host=localhost;Port=5432;Database=kocluk_notification;Username=kemal;Password=kemal1234;";

        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "notification"))
            .Options;

        return new NotificationDbContext(options);
    }
}

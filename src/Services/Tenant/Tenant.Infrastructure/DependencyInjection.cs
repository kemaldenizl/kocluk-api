using BuildingBlocks.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenant.Application.Abstractions.Persistence;
using Tenant.Application.Abstractions.Time;
using Tenant.Application.Abstractions.UnitOfWork;
using Tenant.Application.Common.Tenancy;
using Tenant.Infrastructure.Persistence;
using Tenant.Infrastructure.Persistence.Repositories;
using Tenant.Infrastructure.Persistence.Seed;
using Tenant.Infrastructure.Time;

namespace Tenant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException("Connection string 'Postgres' was not found.");

        services.AddDbContext<TenantDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "tenant"));
        });

        services.Configure<TenantDefaultsOptions>(configuration.GetSection(TenantDefaultsOptions.SectionName));

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<TenantSeeder>();

        // Shared RabbitMQ/MassTransit plumbing (outbox + host + IEventPublisher) from the building block.
        // The Tenant service owns the tenant lifecycle, so it only publishes; it registers no consumers yet.
        services.AddRabbitMqMessaging<TenantDbContext>(configuration);

        return services;
    }
}

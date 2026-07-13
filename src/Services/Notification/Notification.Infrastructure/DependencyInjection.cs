using System.Net.Http.Headers;
using BuildingBlocks.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notification.Application.Abstractions.Email;
using Notification.Application.Abstractions.Persistence;
using Notification.Application.Abstractions.Time;
using Notification.Application.Abstractions.UnitOfWork;
using Notification.Application.Emails;
using Notification.Infrastructure.Dispatch;
using Notification.Infrastructure.Email;
using Notification.Infrastructure.Messaging.Consumers;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Persistence.Repositories;
using Notification.Infrastructure.Time;

namespace Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException("Connection string 'Postgres' was not found.");

        services.AddDbContext<NotificationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "notification"));
        });

        services.Configure<EmailLinkOptions>(configuration.GetSection(EmailLinkOptions.SectionName));
        services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));
        services.Configure<NotificationDispatcherOptions>(configuration.GetSection(NotificationDispatcherOptions.SectionName));

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddHttpClient<IEmailSender, ResendEmailSender>((serviceProvider, client) =>
        {
            var resend = serviceProvider.GetRequiredService<IOptions<ResendOptions>>().Value;
            client.BaseAddress = new Uri(resend.BaseAddress);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", resend.ApiKey);
        });

        services.AddHostedService<NotificationDispatcherService>();

        // Shared RabbitMQ/MassTransit plumbing (outbox + inbox + host) from the building block. Notification
        // stays independent: it owns its DbContext and registers only the consumers it needs.
        services.AddRabbitMqMessaging<NotificationDbContext>(configuration, x =>
        {
            x.AddConsumer<EmailVerificationRequestedConsumer>();
            x.AddConsumer<PasswordResetRequestedConsumer>();
            x.AddConsumer<PasswordChangeRequestedConsumer>();
            x.AddConsumer<EmailChangeRequestedConsumer>();
        });

        return services;
    }
}

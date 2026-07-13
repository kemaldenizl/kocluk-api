using BuildingBlocks.Messaging.Abstractions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Messaging.RabbitMq;

public static class MessagingRegistration
{
    /// <summary>
    /// Registers the shared RabbitMQ/MassTransit plumbing: the transactional EF Core outbox bound to the
    /// caller's <typeparamref name="TDbContext"/>, the RabbitMQ host, and the <see cref="IEventPublisher"/>
    /// implementation. Each service stays independent: it supplies its own DbContext and registers its own
    /// consumers through <paramref name="configure"/>, so no service's messaging surface leaks into another.
    /// </summary>
    /// <param name="configure">
    /// Hook to register service-owned consumers and any bus customisations (e.g. <c>x.AddConsumer&lt;T&gt;()</c>).
    /// </param>
    public static IServiceCollection AddRabbitMqMessaging<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null)
        where TDbContext : DbContext
    {
        var options = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
                      ?? throw new InvalidOperationException("RabbitMq configuration section is missing.");

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<TDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            configure?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Host, options.VirtualHost, host =>
                {
                    host.Username(options.Username);
                    host.Password(options.Password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        return services;
    }
}

using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Behaviors;
using Notification.Application.Emails;

namespace Notification.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

        services.AddSingleton<IEmailTemplateFactory, EmailTemplateFactory>();

        return services;
    }
}

using MassTransit;
using Security.Application.Abstractions.Messaging;

namespace Security.Infrastructure.Messaging;

public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}

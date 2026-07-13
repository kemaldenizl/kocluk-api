using BuildingBlocks.Messaging.Abstractions;
using MassTransit;

namespace BuildingBlocks.Messaging.RabbitMq;

public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}

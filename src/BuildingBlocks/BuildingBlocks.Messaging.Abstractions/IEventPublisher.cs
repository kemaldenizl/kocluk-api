namespace BuildingBlocks.Messaging.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class;
}

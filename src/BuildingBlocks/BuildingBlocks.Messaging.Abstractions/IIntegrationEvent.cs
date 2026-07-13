namespace BuildingBlocks.Messaging.Abstractions;

/// <summary>
/// Marker for integration events — messages published across service boundaries. The contract itself
/// (name, shape) stays owned by the service that publishes it; this abstraction only standardises the
/// common metadata every integration event carries, without coupling services to each other.
/// </summary>
public interface IIntegrationEvent
{
    DateTime OccurredOnUtc { get; }
}

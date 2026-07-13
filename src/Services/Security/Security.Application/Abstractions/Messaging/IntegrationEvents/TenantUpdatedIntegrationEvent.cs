namespace Security.Application.Abstractions.Messaging.IntegrationEvents;

/// <summary>
/// Published by the Tenant microservice when a tenant's mutable state changes (renamed, activated or
/// deactivated). The security service consumes this event to keep its local tenant read model in sync.
/// The full current state is carried so the consumer can upsert idempotently and self-heal if a prior
/// event was missed.
/// </summary>
public sealed record TenantUpdatedIntegrationEvent(
    Guid TenantId,
    string Name,
    string Slug,
    bool IsActive,
    DateTime OccurredOnUtc);

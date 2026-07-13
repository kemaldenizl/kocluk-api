namespace Security.Application.Abstractions.Messaging.IntegrationEvents;

/// <summary>
/// Published by the Tenant microservice (the owner of the tenant lifecycle) when a new tenant is
/// provisioned. The security service consumes this event to build a local read model of tenants,
/// which it uses to validate the tenant on incoming requests. The Tenant microservice must publish
/// a message matching this contract (name and shape) for MassTransit to route it here.
/// </summary>
public sealed record TenantCreatedIntegrationEvent(
    Guid TenantId,
    string Name,
    string Slug,
    DateTime OccurredOnUtc);

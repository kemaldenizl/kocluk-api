using BuildingBlocks.Messaging.Abstractions;

namespace BuildingBlocks.Messaging.Contracts;

/// <summary>
/// Owner: Tenant service. Published when a tenant is removed from the platform. Consumers retire their
/// local tenant read model so the tenant can no longer be resolved on incoming requests.
/// </summary>
public sealed record TenantDeletedIntegrationEvent(
    Guid TenantId,
    DateTime OccurredOnUtc) : IIntegrationEvent;

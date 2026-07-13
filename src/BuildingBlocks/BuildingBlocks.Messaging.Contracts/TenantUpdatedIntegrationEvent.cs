using BuildingBlocks.Messaging.Abstractions;

namespace BuildingBlocks.Messaging.Contracts;

/// <summary>
/// Owner: Tenant service. Published when a tenant's mutable state changes (renamed, activated or
/// deactivated). The full current state is carried so consumers can upsert idempotently and self-heal
/// if a prior event was missed.
/// </summary>
public sealed record TenantUpdatedIntegrationEvent(
    Guid TenantId,
    string Name,
    string Slug,
    bool IsActive,
    DateTime OccurredOnUtc) : IIntegrationEvent;

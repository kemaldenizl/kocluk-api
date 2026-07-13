using BuildingBlocks.Messaging.Abstractions;

namespace BuildingBlocks.Messaging.Contracts;

/// <summary>
/// Owner: Tenant service (the authority for the tenant lifecycle). Published when a new tenant is
/// provisioned. Consumers (e.g. Security) build a local tenant read model used to validate tenants on
/// incoming requests. The contract is shared verbatim between publisher and subscriber so MassTransit
/// routes by a single message URN.
/// </summary>
public sealed record TenantCreatedIntegrationEvent(
    Guid TenantId,
    string Name,
    string Slug,
    DateTime OccurredOnUtc) : IIntegrationEvent;

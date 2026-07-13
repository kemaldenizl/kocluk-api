using BuildingBlocks.Messaging.Abstractions;

namespace BuildingBlocks.Messaging.Contracts;

/// <summary>
/// Owner: Security service. Published when a user requests to change their email address. Consumers send
/// the confirmation mail to the new address carrying <see cref="Token"/>.
/// </summary>
public sealed record EmailChangeRequestedIntegrationEvent(
    Guid TenantId,
    Guid UserId,
    string Email,
    string Token,
    DateTime OccurredOnUtc) : IIntegrationEvent;

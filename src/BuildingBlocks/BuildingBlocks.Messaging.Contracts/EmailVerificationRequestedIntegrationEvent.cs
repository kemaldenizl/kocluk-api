using BuildingBlocks.Messaging.Abstractions;

namespace BuildingBlocks.Messaging.Contracts;

/// <summary>
/// Owner: Security service. Published when a user must verify their email address (registration or a
/// resend request). Consumers (e.g. Notification) send the verification mail carrying <see cref="Token"/>.
/// This contract is shared verbatim between publisher and subscriber so MassTransit routes by a single URN.
/// </summary>
public sealed record EmailVerificationRequestedIntegrationEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTime OccurredOnUtc) : IIntegrationEvent;

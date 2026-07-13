using BuildingBlocks.Messaging.Abstractions;

namespace BuildingBlocks.Messaging.Contracts;

/// <summary>
/// Owner: Security service. Published when an authenticated user requests a password change that must be
/// confirmed by email. Consumers send the confirmation mail carrying <see cref="Token"/>.
/// </summary>
public sealed record PasswordChangeRequestedIntegrationEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTime OccurredOnUtc) : IIntegrationEvent;

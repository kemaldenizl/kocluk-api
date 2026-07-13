using BuildingBlocks.Messaging.Abstractions;

namespace BuildingBlocks.Messaging.Contracts;

/// <summary>
/// Owner: Security service. Published when a user requests a password reset (forgot password). Consumers
/// send the reset mail carrying <see cref="Token"/>.
/// </summary>
public sealed record PasswordResetRequestedIntegrationEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTime OccurredOnUtc) : IIntegrationEvent;

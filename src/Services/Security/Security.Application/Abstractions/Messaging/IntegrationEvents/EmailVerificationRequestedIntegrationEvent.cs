namespace Security.Application.Abstractions.Messaging.IntegrationEvents;

public sealed record EmailVerificationRequestedIntegrationEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTime OccurredOnUtc);

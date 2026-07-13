namespace Security.Application.Abstractions.Messaging.IntegrationEvents;

public sealed record PasswordResetRequestedIntegrationEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTime OccurredOnUtc);

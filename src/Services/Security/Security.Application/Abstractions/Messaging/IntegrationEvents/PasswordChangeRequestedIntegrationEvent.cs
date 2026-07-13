namespace Security.Application.Abstractions.Messaging.IntegrationEvents;

public sealed record PasswordChangeRequestedIntegrationEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTime OccurredOnUtc);

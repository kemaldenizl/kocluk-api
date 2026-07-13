namespace Security.Application.Abstractions.Messaging.IntegrationEvents;

public sealed record EmailChangeRequestedIntegrationEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTime OccurredOnUtc);

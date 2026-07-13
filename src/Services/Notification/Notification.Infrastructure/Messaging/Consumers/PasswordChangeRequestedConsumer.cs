using BuildingBlocks.Messaging.Contracts;
using MediatR;
using Notification.Domain.Notifications;

namespace Notification.Infrastructure.Messaging.Consumers;

public sealed class PasswordChangeRequestedConsumer(ISender sender)
    : EmailNotificationConsumer<PasswordChangeRequestedIntegrationEvent>(sender)
{
    protected override NotificationType Type => NotificationType.PasswordChange;

    protected override (string Email, string Token, DateTime OccurredOnUtc) Extract(
        PasswordChangeRequestedIntegrationEvent message) =>
        (message.Email, message.Token, message.OccurredOnUtc);
}

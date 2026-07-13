using BuildingBlocks.Messaging.Contracts;
using MediatR;
using Notification.Domain.Notifications;

namespace Notification.Infrastructure.Messaging.Consumers;

public sealed class PasswordResetRequestedConsumer(ISender sender)
    : EmailNotificationConsumer<PasswordResetRequestedIntegrationEvent>(sender)
{
    protected override NotificationType Type => NotificationType.PasswordReset;

    protected override (string Email, string Token, DateTime OccurredOnUtc) Extract(
        PasswordResetRequestedIntegrationEvent message) =>
        (message.Email, message.Token, message.OccurredOnUtc);
}

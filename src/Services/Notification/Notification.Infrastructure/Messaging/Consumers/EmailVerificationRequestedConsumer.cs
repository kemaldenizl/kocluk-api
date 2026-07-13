using BuildingBlocks.Messaging.Contracts;
using MediatR;
using Notification.Domain.Notifications;

namespace Notification.Infrastructure.Messaging.Consumers;

public sealed class EmailVerificationRequestedConsumer(ISender sender)
    : EmailNotificationConsumer<EmailVerificationRequestedIntegrationEvent>(sender)
{
    protected override NotificationType Type => NotificationType.EmailVerification;

    protected override (Guid TenantId, string Email, string Token, DateTime OccurredOnUtc) Extract(
        EmailVerificationRequestedIntegrationEvent message) =>
        (message.TenantId, message.Email, message.Token, message.OccurredOnUtc);
}

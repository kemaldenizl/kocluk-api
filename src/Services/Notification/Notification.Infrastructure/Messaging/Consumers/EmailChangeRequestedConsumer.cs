using BuildingBlocks.Messaging.Contracts;
using MediatR;
using Notification.Domain.Notifications;

namespace Notification.Infrastructure.Messaging.Consumers;

public sealed class EmailChangeRequestedConsumer(ISender sender)
    : EmailNotificationConsumer<EmailChangeRequestedIntegrationEvent>(sender)
{
    protected override NotificationType Type => NotificationType.EmailChange;

    protected override (Guid TenantId, string Email, string Token, DateTime OccurredOnUtc) Extract(
        EmailChangeRequestedIntegrationEvent message) =>
        (message.TenantId, message.Email, message.Token, message.OccurredOnUtc);
}

using MassTransit;
using MediatR;
using Notification.Application.Notifications.QueueNotification;
using Notification.Domain.Notifications;

namespace Notification.Infrastructure.Messaging.Consumers;

/// <summary>
/// Shared consumer logic for the transactional-email integration events. Each concrete consumer only maps
/// its event type to a <see cref="NotificationType"/> and extracts the common fields; queuing, idempotency
/// key derivation and dispatch are handled uniformly here (DRY). The MassTransit EF inbox additionally
/// de-duplicates broker redeliveries.
/// </summary>
public abstract class EmailNotificationConsumer<TEvent>(ISender sender) : IConsumer<TEvent>
    where TEvent : class
{
    protected abstract NotificationType Type { get; }

    protected abstract (string Email, string Token, DateTime OccurredOnUtc) Extract(TEvent message);

    public Task Consume(ConsumeContext<TEvent> context)
    {
        var (email, token, occurredOnUtc) = Extract(context.Message);

        // Deterministic key: the token is unique per request, so replays collapse onto the same notification.
        var idempotencyKey = $"{Type}:{token}";

        var command = new QueueNotificationCommand(Type, email, token, occurredOnUtc, idempotencyKey);

        return sender.Send(command, context.CancellationToken);
    }
}

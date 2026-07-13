using MediatR;
using Notification.Domain.Notifications;

namespace Notification.Application.Notifications.QueueNotification;

/// <summary>
/// Captures an inbound integration event as a durable, pending notification. A single command handles every
/// email type (DRY): consumers translate their event into this shape. The <see cref="IdempotencyKey"/> is
/// derived deterministically from the source event so replays are de-duplicated.
/// </summary>
public sealed record QueueNotificationCommand(
    NotificationType Type,
    string Recipient,
    string Token,
    DateTime OccurredOnUtc,
    string IdempotencyKey) : IRequest;

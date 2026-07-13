namespace Notification.Domain.Notifications;

/// <summary>
/// Lifecycle of a notification within the store-and-forward outbox. A row is created <see cref="Pending"/>,
/// dispatched to the email provider, then settles as <see cref="Sent"/> or, after exhausting retries,
/// <see cref="DeadLettered"/>. <see cref="Failed"/> is a transient state between retry attempts.
/// </summary>
public enum NotificationStatus
{
    Pending = 1,
    Failed = 2,
    Sent = 3,
    DeadLettered = 4
}

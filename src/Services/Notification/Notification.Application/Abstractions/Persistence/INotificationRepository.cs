using DomainNotification = Notification.Domain.Notifications.Notification;

namespace Notification.Application.Abstractions.Persistence;

public interface INotificationRepository
{
    Task AddAsync(DomainNotification notification, CancellationToken cancellationToken = default);

    /// <summary>True if a notification with the given idempotency key already exists (dedup guard).</summary>
    Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a batch of notifications that are due for a delivery attempt: those still
    /// <c>Pending</c> or <c>Failed</c> whose <c>NextAttemptAtUtc</c> has elapsed, oldest first.
    /// </summary>
    Task<IReadOnlyList<DomainNotification>> GetDispatchBatchAsync(
        DateTime nowUtc,
        int batchSize,
        CancellationToken cancellationToken = default);
}

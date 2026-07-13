using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions.Persistence;
using Notification.Domain.Notifications;
using DomainNotification = Notification.Domain.Notifications.Notification;

namespace Notification.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository(NotificationDbContext dbContext) : INotificationRepository
{
    public Task AddAsync(DomainNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return dbContext.Notifications.AddAsync(notification, cancellationToken).AsTask();
    }

    public Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return dbContext.Notifications
            .AsNoTracking()
            .AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<IReadOnlyList<DomainNotification>> GetDispatchBatchAsync(
        DateTime nowUtc,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .Where(x => (x.Status == NotificationStatus.Pending || x.Status == NotificationStatus.Failed)
                        && x.NextAttemptAtUtc <= nowUtc)
            .OrderBy(x => x.NextAttemptAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}

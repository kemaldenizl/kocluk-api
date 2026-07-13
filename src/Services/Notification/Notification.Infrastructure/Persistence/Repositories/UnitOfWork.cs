using Notification.Application.Abstractions.UnitOfWork;

namespace Notification.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(NotificationDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

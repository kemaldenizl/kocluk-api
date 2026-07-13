using Tenant.Application.Abstractions.UnitOfWork;

namespace Tenant.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(TenantDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using Security.Application.Abstractions.Persistence;
using Security.Domain.Memberships;

namespace Security.Infrastructure.Persistence.Repositories;

public sealed class MembershipRepository(SecurityDbContext dbContext) : IMembershipRepository
{
    public async Task<Membership?> GetByUserAndTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Memberships
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.TenantId == tenantId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Membership>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Memberships
            .Include(x => x.Roles)
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Membership>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Memberships
            .Include(x => x.Roles)
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Membership membership, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(membership);

        return dbContext.Memberships.AddAsync(membership, cancellationToken).AsTask();
    }
}

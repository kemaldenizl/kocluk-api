using Microsoft.EntityFrameworkCore;
using Security.Application.Abstractions.Persistence;
using Security.Domain.Authorization;
using Security.Domain.Memberships;

namespace Security.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository(SecurityDbContext dbContext) : IRoleRepository
{
    public async Task<Role?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .Include(x => x.Permissions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Role?> GetByTenantAndNormalizedNameAsync(
        Guid tenantId,
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .Include(x => x.Permissions)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.NormalizedName == normalizedName, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Role>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .AsNoTracking()
            .Include(x => x.Permissions)
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByTenantAndNormalizedNameAsync(
        Guid tenantId,
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.NormalizedName == normalizedName, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetActiveUserIdsByRoleIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from membershipRole in dbContext.MembershipRoles.AsNoTracking()
            join membership in dbContext.Memberships.AsNoTracking()
                on membershipRole.MembershipId equals membership.Id
            where membershipRole.RoleId == roleId && membership.Status == MembershipStatus.Active
            select membership.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        return dbContext.Roles.AddAsync(role, cancellationToken).AsTask();
    }

    public void Remove(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);

        dbContext.Roles.Remove(role);
    }
}

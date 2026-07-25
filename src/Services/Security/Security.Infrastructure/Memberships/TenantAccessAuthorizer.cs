using Microsoft.EntityFrameworkCore;
using Security.Application.Abstractions.Memberships;
using Security.Domain.Authorization;
using Security.Domain.Memberships;
using Security.Infrastructure.Persistence;

namespace Security.Infrastructure.Memberships;

public sealed class TenantAccessAuthorizer(SecurityDbContext dbContext) : ITenantAccessAuthorizer
{
    public async Task<bool> IsSystemAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await (
            from upr in dbContext.UserPlatformRoles.AsNoTracking()
            join r in dbContext.Roles.AsNoTracking() on upr.RoleId equals r.Id
            where upr.UserId == userId && r.NormalizedName == PlatformRoles.SystemAdminNormalized
            select upr.UserId)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> CanAccessTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (await IsSystemAdminAsync(userId, cancellationToken))
            return true;

        return await dbContext.Memberships
            .AsNoTracking()
            .AnyAsync(
                m => m.UserId == userId && m.TenantId == tenantId && m.Status == MembershipStatus.Active,
                cancellationToken);
    }

    public async Task<bool> HasTenantPermissionAsync(
        Guid userId,
        Guid tenantId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        if (await IsSystemAdminAsync(userId, cancellationToken))
            return true;

        var roleIds = await dbContext.Memberships
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.TenantId == tenantId && m.Status == MembershipStatus.Active)
            .SelectMany(m => m.Roles.Select(r => r.RoleId))
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
            return false;

        return await (
            from rp in dbContext.RolePermissions.AsNoTracking()
            join p in dbContext.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
            where roleIds.Contains(rp.RoleId) && p.Code == permissionCode
            select rp.RoleId)
            .AnyAsync(cancellationToken);
    }
}

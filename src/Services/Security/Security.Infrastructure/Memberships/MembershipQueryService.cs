using Microsoft.EntityFrameworkCore;
using Security.Application.Abstractions.Memberships;
using Security.Application.Memberships.Dtos;
using Security.Domain.Authorization;
using Security.Domain.Memberships;
using Security.Infrastructure.Persistence;

namespace Security.Infrastructure.Memberships;

/// <summary>
/// Read-side implementation that assembles a user's access context directly from the security store.
/// Only <see cref="MembershipStatus.Active"/> memberships contribute to the token; suspended/removed
/// memberships are excluded so they neither grant permissions nor appear in the memberships claim.
/// </summary>
public sealed class MembershipQueryService(SecurityDbContext dbContext) : IMembershipQueryService
{
    public async Task<UserAccessContext> GetAccessContextAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await dbContext.Memberships
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Status == MembershipStatus.Active)
            .Select(m => new
            {
                m.TenantId,
                RoleIds = m.Roles.Select(r => r.RoleId).ToList()
            })
            .ToListAsync(cancellationToken);

        var tenantRoleIds = memberships.SelectMany(m => m.RoleIds).Distinct().ToList();

        var tenantRoleNames = await dbContext.Roles
            .AsNoTracking()
            .Where(r => tenantRoleIds.Contains(r.Id))
            .Select(r => new { r.Id, r.Name })
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var platformRoles = await (
            from upr in dbContext.UserPlatformRoles.AsNoTracking()
            join r in dbContext.Roles.AsNoTracking() on upr.RoleId equals r.Id
            where upr.UserId == userId
            select new { r.Id, r.Name, r.NormalizedName })
            .ToListAsync(cancellationToken);

        var permissionRoleIds = tenantRoleIds
            .Concat(platformRoles.Select(r => r.Id))
            .Distinct()
            .ToList();

        var permissions = await (
            from rp in dbContext.RolePermissions.AsNoTracking()
            join p in dbContext.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
            where permissionRoleIds.Contains(rp.RoleId)
            select p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var membershipAccess = memberships
            .Select(m => new TenantMembershipAccess(
                m.TenantId,
                m.RoleIds
                    .Where(tenantRoleNames.ContainsKey)
                    .Select(id => tenantRoleNames[id])
                    .ToList(),
                MembershipStatus.Active.ToString()))
            .ToList();

        var isSystemAdmin = platformRoles
            .Any(r => string.Equals(r.NormalizedName, PlatformRoles.SystemAdminNormalized, StringComparison.OrdinalIgnoreCase));

        return new UserAccessContext(
            membershipAccess,
            platformRoles.Select(r => r.Name).ToList(),
            isSystemAdmin,
            permissions);
    }

    public async Task<IReadOnlyList<MembershipDto>> GetUserMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await dbContext.Memberships
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => new
            {
                m.Id,
                m.UserId,
                m.TenantId,
                m.Status,
                m.JoinedAtUtc,
                m.UpdatedAtUtc,
                RoleIds = m.Roles.Select(r => r.RoleId).ToList()
            })
            .ToListAsync(cancellationToken);

        var roleNames = await GetRoleNamesAsync(
            memberships.SelectMany(m => m.RoleIds),
            cancellationToken);

        return memberships
            .Select(m => new MembershipDto(
                m.Id,
                m.UserId,
                m.TenantId,
                m.Status.ToString(),
                m.RoleIds.Where(roleNames.ContainsKey).Select(id => roleNames[id]).ToList(),
                m.JoinedAtUtc,
                m.UpdatedAtUtc))
            .ToList();
    }

    public async Task<IReadOnlyList<TenantMemberDto>> GetTenantMembersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var members = await (
            from m in dbContext.Memberships.AsNoTracking()
            join u in dbContext.Users.AsNoTracking() on m.UserId equals u.Id
            where m.TenantId == tenantId
            select new
            {
                m.UserId,
                u.Email,
                u.FirstName,
                u.LastName,
                m.Status,
                m.JoinedAtUtc,
                m.UpdatedAtUtc,
                RoleIds = m.Roles.Select(r => r.RoleId).ToList()
            })
            .ToListAsync(cancellationToken);

        var roleNames = await GetRoleNamesAsync(
            members.SelectMany(m => m.RoleIds),
            cancellationToken);

        return members
            .Select(m => new TenantMemberDto(
                m.UserId,
                m.Email,
                m.FirstName,
                m.LastName,
                m.Status.ToString(),
                m.RoleIds.Where(roleNames.ContainsKey).Select(id => roleNames[id]).ToList(),
                m.JoinedAtUtc,
                m.UpdatedAtUtc))
            .ToList();
    }

    private async Task<Dictionary<Guid, string>> GetRoleNamesAsync(
        IEnumerable<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        var ids = roleIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        return await dbContext.Roles
            .AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .Select(r => new { r.Id, r.Name })
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);
    }
}

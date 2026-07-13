using Microsoft.EntityFrameworkCore;
using Security.Application.Abstractions.Persistence;
using Security.Domain.Authorization;

namespace Security.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository(SecurityDbContext dbContext) : IRoleRepository
{
    public async Task<Role?> GetByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .Include(x => x.Permissions)
            .FirstOrDefaultAsync(x => x.NormalizedName == normalizedName, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionCodesByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var permissionCodes = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join rolePermission in dbContext.RolePermissions.AsNoTracking()
                on userRole.RoleId equals rolePermission.RoleId
            join permission in dbContext.Permissions.AsNoTracking()
                on rolePermission.PermissionId equals permission.Id
            where userRole.UserId == userId
            select permission.Code
        )
        .Distinct()
        .ToListAsync(cancellationToken);

        return permissionCodes;
    }

    public async Task<Role?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .Include(x => x.Permissions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Role>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .AsNoTracking()
            .Include(x => x.Permissions)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .AsNoTracking()
            .AnyAsync(x => x.NormalizedName == normalizedName, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetUserIdsByRoleIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UserRoles
            .AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .Select(x => x.UserId)
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

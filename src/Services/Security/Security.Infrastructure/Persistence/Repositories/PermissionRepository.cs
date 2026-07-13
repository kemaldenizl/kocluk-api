using Microsoft.EntityFrameworkCore;
using Security.Application.Abstractions.Persistence;
using Security.Domain.Authorization;

namespace Security.Infrastructure.Persistence.Repositories;

public sealed class PermissionRepository(SecurityDbContext dbContext) : IPermissionRepository
{
    public async Task<Permission?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Permissions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Permission>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Permissions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Permissions
            .AsNoTracking()
            .AnyAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<bool> IsAssignedToAnyRoleAsync(
        Guid permissionId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.RolePermissions
            .AsNoTracking()
            .AnyAsync(x => x.PermissionId == permissionId, cancellationToken);
    }

    public Task AddAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permission);

        return dbContext.Permissions.AddAsync(permission, cancellationToken).AsTask();
    }

    public void Remove(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        dbContext.Permissions.Remove(permission);
    }
}

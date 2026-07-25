using Microsoft.EntityFrameworkCore;
using Security.Application.Abstractions.Persistence;
using Security.Domain.Authorization;

namespace Security.Infrastructure.Persistence.Repositories;

public sealed class UserPlatformRoleRepository(SecurityDbContext dbContext) : IUserPlatformRoleRepository
{
    public async Task<IReadOnlyCollection<UserPlatformRole>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UserPlatformRoles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UserPlatformRoles
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.RoleId == roleId, cancellationToken);
    }

    public Task AddAsync(UserPlatformRole userPlatformRole, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userPlatformRole);

        return dbContext.UserPlatformRoles.AddAsync(userPlatformRole, cancellationToken).AsTask();
    }
}

using Security.Domain.Authorization;

namespace Security.Application.Abstractions.Persistence;

public interface IUserPlatformRoleRepository
{
    Task<IReadOnlyCollection<UserPlatformRole>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    Task AddAsync(UserPlatformRole userPlatformRole, CancellationToken cancellationToken = default);
}

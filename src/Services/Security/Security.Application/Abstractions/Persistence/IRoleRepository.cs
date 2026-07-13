using Security.Domain.Authorization;

namespace Security.Application.Abstractions.Persistence;

public interface IRoleRepository
{
    Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetPermissionCodesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Role>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> GetUserIdsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task AddAsync(Role role, CancellationToken cancellationToken = default);

    void Remove(Role role);
}

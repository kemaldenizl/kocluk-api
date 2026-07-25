using Security.Domain.Authorization;

namespace Security.Application.Abstractions.Persistence;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Role?> GetByTenantAndNormalizedNameAsync(Guid tenantId, string normalizedName, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Role>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByTenantAndNormalizedNameAsync(Guid tenantId, string normalizedName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ids of the users who currently hold this role through an <b>active</b> membership. Used to
    /// invalidate their access tokens when a role's permissions change.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> GetActiveUserIdsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task AddAsync(Role role, CancellationToken cancellationToken = default);

    void Remove(Role role);
}

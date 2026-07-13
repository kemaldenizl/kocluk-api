using Security.Domain.Authorization;

namespace Security.Application.Abstractions.Persistence;

public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Permission>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> IsAssignedToAnyRoleAsync(Guid permissionId, CancellationToken cancellationToken = default);

    Task AddAsync(Permission permission, CancellationToken cancellationToken = default);

    void Remove(Permission permission);
}

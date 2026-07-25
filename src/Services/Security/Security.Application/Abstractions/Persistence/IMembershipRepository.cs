using Security.Domain.Memberships;

namespace Security.Application.Abstractions.Persistence;

public interface IMembershipRepository
{
    /// <summary>Gets the membership (any status) of a user in a tenant, including its roles.</summary>
    Task<Membership?> GetByUserAndTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Gets all memberships of a user across every tenant (all statuses, history included).</summary>
    Task<IReadOnlyCollection<Membership>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Gets all memberships belonging to a tenant (all statuses), including their roles.</summary>
    Task<IReadOnlyCollection<Membership>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task AddAsync(Membership membership, CancellationToken cancellationToken = default);
}

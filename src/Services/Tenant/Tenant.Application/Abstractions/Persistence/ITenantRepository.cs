using DomainTenant = Tenant.Domain.Tenants.Tenant;

namespace Tenant.Application.Abstractions.Persistence;

public interface ITenantRepository
{
    Task<DomainTenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DomainTenant>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task AddAsync(DomainTenant tenant, CancellationToken cancellationToken = default);

    void Remove(DomainTenant tenant);
}

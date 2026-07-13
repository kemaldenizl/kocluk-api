using Security.Domain.Tenancy;

namespace Security.Application.Abstractions.Persistence;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> ExistsActiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
}

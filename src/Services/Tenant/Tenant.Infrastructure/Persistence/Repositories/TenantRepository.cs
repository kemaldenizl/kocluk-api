using Microsoft.EntityFrameworkCore;
using Tenant.Application.Abstractions.Persistence;
using DomainTenant = Tenant.Domain.Tenants.Tenant;

namespace Tenant.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository(TenantDbContext dbContext) : ITenantRepository
{
    public async Task<DomainTenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Tenants
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DomainTenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Tenants
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        return dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(x => x.Slug == normalizedSlug, cancellationToken);
    }

    public Task AddAsync(DomainTenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        return dbContext.Tenants.AddAsync(tenant, cancellationToken).AsTask();
    }

    public void Remove(DomainTenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        dbContext.Tenants.Remove(tenant);
    }
}

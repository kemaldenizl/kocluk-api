using Microsoft.EntityFrameworkCore;
using Security.Application.Abstractions.Persistence;
using Security.Domain.Tenancy;

namespace Security.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository(SecurityDbContext dbContext) : ITenantRepository
{
    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Tenants
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        return await dbContext.Tenants
            .FirstOrDefaultAsync(x => x.Slug == normalizedSlug, cancellationToken);
    }

    public async Task<bool> ExistsActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);
    }

    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        return dbContext.Tenants.AddAsync(tenant, cancellationToken).AsTask();
    }
}

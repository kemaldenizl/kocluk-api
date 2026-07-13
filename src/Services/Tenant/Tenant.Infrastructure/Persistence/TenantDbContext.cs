using MassTransit;
using Microsoft.EntityFrameworkCore;
using DomainTenant = Tenant.Domain.Tenants.Tenant;

namespace Tenant.Infrastructure.Persistence;

public sealed class TenantDbContext(DbContextOptions<TenantDbContext> options) : DbContext(options)
{
    public DbSet<DomainTenant> Tenants => Set<DomainTenant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("tenant");

        builder.ApplyConfigurationsFromAssembly(typeof(TenantDbContext).Assembly);

        // MassTransit transactional outbox: lifecycle events are persisted in the same transaction as the
        // tenant change and delivered reliably. The inbox de-duplicates any events this service consumes later.
        builder.AddInboxStateEntity();
        builder.AddOutboxMessageEntity();
        builder.AddOutboxStateEntity();
    }
}

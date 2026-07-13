using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Security.Application.Abstractions.Tenancy;
using Security.Domain.Abstractions;

namespace Security.Infrastructure.Tenancy;

/// <summary>
/// Stamps the current tenant onto newly inserted tenant-scoped entities and guards against
/// cross-tenant writes. This keeps tenant assignment out of the domain/application layers:
/// handlers construct entities without ever knowing the tenant, and isolation is enforced here.
/// </summary>
public sealed class TenantSaveChangesInterceptor(ITenantContext tenantContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyTenant(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTenant(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyTenant(DbContext? context)
    {
        if (context is null)
            return;

        var tenantId = tenantContext.TenantId;

        foreach (var entry in context.ChangeTracker.Entries<ITenantScoped>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    SetTenantId(entry, tenantId);
                    break;

                case EntityState.Modified:
                case EntityState.Deleted:
                    GuardSameTenant(entry, tenantId);
                    break;
            }
        }
    }

    private static void SetTenantId(EntityEntry<ITenantScoped> entry, Guid tenantId)
    {
        entry.Property(x => x.TenantId).CurrentValue = tenantId;
    }

    private static void GuardSameTenant(EntityEntry<ITenantScoped> entry, Guid tenantId)
    {
        var current = entry.Property(x => x.TenantId).CurrentValue;
        if (current != tenantId)
        {
            throw new InvalidOperationException(
                $"Cross-tenant modification detected for '{entry.Entity.GetType().Name}'. " +
                $"Current tenant '{tenantId}' cannot modify data owned by tenant '{current}'.");
        }
    }
}

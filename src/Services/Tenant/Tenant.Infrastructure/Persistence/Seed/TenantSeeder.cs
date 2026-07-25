using BuildingBlocks.Messaging.Abstractions;
using BuildingBlocks.Messaging.Contracts;
using Microsoft.Extensions.Options;
using Tenant.Application.Abstractions.Persistence;
using Tenant.Application.Abstractions.Time;
using Tenant.Application.Abstractions.UnitOfWork;
using Tenant.Application.Common.Tenancy;
using DomainTenant = Tenant.Domain.Tenants.Tenant;

namespace Tenant.Infrastructure.Persistence.Seed;

/// <summary>
/// Ensures the well-known public tenant exists. The Tenant service is the authority for the tenant
/// lifecycle, so it provisions the public tenant with a fixed id and publishes
/// <see cref="TenantCreatedIntegrationEvent"/> (through the outbox) so other services sync their read models.
/// </summary>
public sealed class TenantSeeder(
    ITenantRepository tenantRepository,
    IEventPublisher eventPublisher,
    IDateTimeProvider dateTimeProvider,
    IOptions<TenantDefaultsOptions> tenantDefaults,
    IUnitOfWork unitOfWork)
{
    private readonly TenantDefaultsOptions _tenantDefaults = tenantDefaults.Value;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var publicTenantId = _tenantDefaults.PublicTenantId == Guid.Empty
            ? TenantDefaultsOptions.PublicTenantIdValue
            : _tenantDefaults.PublicTenantId;

        var existing = await tenantRepository.GetByIdAsync(publicTenantId, cancellationToken);
        if (existing is not null)
            return;

        var utcNow = dateTimeProvider.UtcNow;

        var tenant = new DomainTenant(
            publicTenantId,
            _tenantDefaults.PublicTenantName,
            _tenantDefaults.PublicTenantSlug,
            utcNow);

        await tenantRepository.AddAsync(tenant, cancellationToken);

        await eventPublisher.PublishAsync(
            new TenantCreatedIntegrationEvent(tenant.Id, tenant.Name, tenant.Slug, utcNow),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

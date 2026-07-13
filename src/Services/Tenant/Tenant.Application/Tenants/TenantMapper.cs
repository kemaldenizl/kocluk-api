using Tenant.Application.Tenants.Dtos;
using DomainTenant = Tenant.Domain.Tenants.Tenant;

namespace Tenant.Application.Tenants;

public static class TenantMapper
{
    public static TenantDto ToDto(this DomainTenant tenant) => new(
        tenant.Id,
        tenant.Name,
        tenant.Slug,
        tenant.IsActive,
        tenant.CreatedAtUtc,
        tenant.UpdatedAtUtc);
}

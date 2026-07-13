using Microsoft.Extensions.Options;
using Security.Application.Abstractions.Tenancy;

namespace Security.Infrastructure.Tenancy;

/// <summary>
/// Scoped, mutable holder for the current tenant. In single-tenant mode it is permanently pinned to
/// the configured default tenant and considered resolved. In multi-tenant mode it starts on the default
/// tenant and is overwritten by the resolution pipeline (middleware) once the real tenant is known.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private readonly bool _isMultiTenant;

    public TenantContext(IOptions<MultiTenancyOptions> options)
    {
        var value = options.Value;
        _isMultiTenant = value.Enabled;

        TenantId = value.DefaultTenantId == Guid.Empty
            ? MultiTenancyOptions.DefaultTenantIdValue
            : value.DefaultTenantId;

        // Single-tenant deployments never resolve a tenant per request; they are always "resolved".
        IsResolved = !value.Enabled;
    }

    public Guid TenantId { get; private set; }

    public bool IsMultiTenant => _isMultiTenant;

    public bool IsResolved { get; private set; }

    public void SetTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant id cannot be empty.", nameof(tenantId));

        TenantId = tenantId;
        IsResolved = true;
    }
}

namespace Security.Application.Abstractions.Tenancy;

/// <summary>
/// Ambient, per-request accessor for the tenant the current operation runs under.
/// It is resolved once per request (from the JWT <c>tenant_id</c> claim for authenticated
/// calls, or the tenant header for anonymous calls) and then consumed by the persistence
/// layer's global query filters and save interceptor. In single-tenant deployments it always
/// resolves to the configured default tenant.
/// </summary>
public interface ITenantContext
{
    /// <summary>The tenant the current request/operation is scoped to.</summary>
    Guid TenantId { get; }

    /// <summary><c>true</c> when the service runs in multi-tenant mode.</summary>
    bool IsMultiTenant { get; }

    /// <summary><c>true</c> once a concrete tenant has been resolved for this scope.</summary>
    bool IsResolved { get; }

    /// <summary>Assigns the resolved tenant for the current scope. Intended for the resolution pipeline.</summary>
    void SetTenant(Guid tenantId);
}

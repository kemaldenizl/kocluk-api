namespace Security.Application.Abstractions.Memberships;

/// <summary>
/// Server-side tenant authorization. Because access tokens carry the union of a user's permissions
/// across all tenants (not per-tenant), tenant-scoped management decisions are verified against the
/// database here: a system administrator may act on any tenant, otherwise the caller must hold the
/// required permission through an active membership in that specific tenant.
/// </summary>
public interface ITenantAccessAuthorizer
{
    Task<bool> IsSystemAdminAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>True when the user is a system admin or an active member of the tenant.</summary>
    Task<bool> CanAccessTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the user is a system admin, or holds a role granting <paramref name="permissionCode"/>
    /// through an active membership in the tenant.
    /// </summary>
    Task<bool> HasTenantPermissionAsync(Guid userId, Guid tenantId, string permissionCode, CancellationToken cancellationToken = default);
}

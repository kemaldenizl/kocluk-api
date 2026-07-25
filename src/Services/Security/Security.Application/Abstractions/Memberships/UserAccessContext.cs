namespace Security.Application.Abstractions.Memberships;

/// <summary>
/// The complete tenant-access picture of a user, assembled for embedding into an access token: the
/// tenants the user actively belongs to (with their per-tenant roles), the tenant-independent platform
/// roles, whether the user is a system administrator, and the flattened union of all permissions.
/// </summary>
public sealed record UserAccessContext(
    IReadOnlyList<TenantMembershipAccess> Memberships,
    IReadOnlyList<string> PlatformRoles,
    bool IsSystemAdmin,
    IReadOnlyCollection<string> Permissions);

/// <summary>A single active membership: the tenant, the roles held in it and its status.</summary>
public sealed record TenantMembershipAccess(
    Guid TenantId,
    IReadOnlyList<string> Roles,
    string Status);

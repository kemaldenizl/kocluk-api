namespace Security.Domain.Authorization;

/// <summary>
/// Determines the reach of a role. Tenant roles belong to a single tenant and are granted through
/// memberships; platform roles are tenant-independent and are granted directly to a user (e.g. the
/// system administrator that can access and manage every tenant).
/// </summary>
public enum RoleScope
{
    Tenant = 1,
    Platform = 2
}

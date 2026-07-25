namespace BuildingBlocks.Authorization;

/// <summary>
/// Canonical permission codes issued in the access token's <c>permission</c> claims and used as
/// authorization policy names across every service. This is the single source of truth: the
/// Security service seeds and issues them, and any microservice protects its routes against them.
/// </summary>
public static class PermissionCodes
{
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";

    public const string RolesRead = "roles.read";
    public const string RolesManage = "roles.manage";

    public const string PermissionsRead = "permissions.read";
    public const string PermissionsManage = "permissions.manage";

    public const string SessionsRead = "sessions.read";
    public const string SessionsManage = "sessions.manage";

    public const string MembersRead = "members.read";
    public const string MembersManage = "members.manage";

    public const string TenantsRead = "tenants.read";
    public const string TenantsManage = "tenants.manage";
}

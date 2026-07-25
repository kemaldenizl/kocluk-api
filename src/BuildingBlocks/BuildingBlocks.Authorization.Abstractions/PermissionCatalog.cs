namespace BuildingBlocks.Authorization;

/// <summary>
/// The complete set of known permission codes. Consumed as the single source of truth by both the
/// Security identity seeder (which persists them) and <c>AddPermissionAuthorization</c> (which
/// registers one authorization policy per code), so the list is never duplicated per call site.
/// </summary>
public static class PermissionCatalog
{
    public static IReadOnlyList<string> All { get; } =
    [
        PermissionCodes.UsersRead,
        PermissionCodes.UsersManage,
        PermissionCodes.RolesRead,
        PermissionCodes.RolesManage,
        PermissionCodes.PermissionsRead,
        PermissionCodes.PermissionsManage,
        PermissionCodes.SessionsRead,
        PermissionCodes.SessionsManage,
        PermissionCodes.MembersRead,
        PermissionCodes.MembersManage,
        PermissionCodes.TenantsRead,
        PermissionCodes.TenantsManage
    ];
}

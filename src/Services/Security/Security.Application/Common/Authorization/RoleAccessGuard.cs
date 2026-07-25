using Security.Application.Abstractions.Memberships;
using Security.Domain.Authorization;

namespace Security.Application.Common.Authorization;

/// <summary>
/// Central rule for who may act on a role: platform roles require the platform system administrator,
/// tenant roles require the given permission through an active membership in the role's tenant.
/// </summary>
internal static class RoleAccessGuard
{
    public static Task<bool> CanActAsync(
        ITenantAccessAuthorizer authorizer,
        Role role,
        Guid callerUserId,
        string tenantPermissionCode,
        CancellationToken cancellationToken)
    {
        return role.Scope == RoleScope.Platform || role.TenantId is null
            ? authorizer.IsSystemAdminAsync(callerUserId, cancellationToken)
            : authorizer.HasTenantPermissionAsync(callerUserId, role.TenantId.Value, tenantPermissionCode, cancellationToken);
    }
}

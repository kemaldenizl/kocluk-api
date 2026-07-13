using Security.Application.Permissions.Dtos;
using Security.Application.Roles.Dtos;
using Security.Domain.Authorization;

namespace Security.Application.Roles;

internal static class RoleMapper
{
    public static RoleDto ToDto(Role role, IReadOnlyDictionary<Guid, string> permissionCodesById)
    {
        var permissions = role.Permissions
            .Select(rolePermission => new PermissionDto(
                rolePermission.PermissionId,
                permissionCodesById.TryGetValue(rolePermission.PermissionId, out var code)
                    ? code
                    : string.Empty))
            .ToArray();

        return new RoleDto(role.Id, role.Name, permissions);
    }
}

using Security.API.Contracts.Permissions;

namespace Security.API.Contracts.Roles;

public sealed record RoleResponse(
    Guid Id,
    string Name,
    IReadOnlyCollection<PermissionResponse> Permissions
);

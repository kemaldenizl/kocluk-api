using Security.Application.Permissions.Dtos;

namespace Security.Application.Roles.Dtos;

public sealed record RoleDto(
    Guid Id,
    string Name,
    IReadOnlyCollection<PermissionDto> Permissions
);

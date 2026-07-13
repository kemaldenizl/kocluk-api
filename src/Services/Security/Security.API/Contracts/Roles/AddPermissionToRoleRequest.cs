namespace Security.API.Contracts.Roles;

public sealed record AddPermissionToRoleRequest(
    Guid PermissionId
);

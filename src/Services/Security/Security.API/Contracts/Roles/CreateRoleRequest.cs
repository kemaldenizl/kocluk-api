namespace Security.API.Contracts.Roles;

public sealed record CreateRoleRequest(
    Guid TenantId,
    string Name
);

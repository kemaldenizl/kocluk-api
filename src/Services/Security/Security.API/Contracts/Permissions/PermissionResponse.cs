namespace Security.API.Contracts.Permissions;

public sealed record PermissionResponse(
    Guid Id,
    string Code
);

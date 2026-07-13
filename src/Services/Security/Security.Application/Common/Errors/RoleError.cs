namespace Security.Application.Common.Errors;

public static class RoleErrors
{
    public static readonly Error NotFound = new("role.not_found", "Role was not found.");
    public static readonly Error AlreadyExists = new("role.already_exists", "A role with the same name already exists.");
    public static readonly Error PermissionNotFound = new("role.permission_not_found", "The specified permission was not found.");
}

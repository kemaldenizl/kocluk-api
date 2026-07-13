namespace Security.Application.Common.Errors;

public static class PermissionErrors
{
    public static readonly Error NotFound = new("permission.not_found", "Permission was not found.");
    public static readonly Error AlreadyExists = new("permission.already_exists", "A permission with the same code already exists.");
    public static readonly Error InUse = new("permission.in_use", "Permission cannot be deleted while it is assigned to a role.");
}

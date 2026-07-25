namespace Security.Application.Common.Errors;

public static class AccessErrors
{
    public static readonly Error Forbidden = new(
        "access.forbidden",
        "You do not have permission to perform this action on this tenant.");
}

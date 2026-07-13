namespace Security.Application.Common.Errors;

public static class UserErrors
{
    public static readonly Error NotFound = new("user.not_found", "User was not found.");
}

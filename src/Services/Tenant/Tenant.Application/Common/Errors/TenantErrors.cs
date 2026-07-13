namespace Tenant.Application.Common.Errors;

/// <summary>Well-known domain errors for tenant operations. Codes are mapped to HTTP status at the API edge.</summary>
public static class TenantErrors
{
    public static readonly Error NotFound = new(
        "tenant.not_found",
        "The specified tenant does not exist.");

    public static readonly Error SlugAlreadyExists = new(
        "tenant.slug_already_exists",
        "A tenant with the same slug already exists.");
}

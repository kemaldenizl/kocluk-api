namespace Security.Application.Common.Errors;

public static class TenantErrors
{
    public static readonly Error NotFound = new("tenant.not_found", "Tenant was not found.");
    public static readonly Error Inactive = new("tenant.inactive", "Tenant is inactive.");
}

namespace Tenant.API.Authorization;

/// <summary>Claim types and policy names shared with the Security-issued access token.</summary>
public static class AuthorizationPolicies
{
    /// <summary>Boolean claim set to <c>true</c> for platform system administrators.</summary>
    public const string SystemAdminClaim = "system_admin";

    /// <summary>Policy requiring the caller to be a platform system administrator.</summary>
    public const string SystemAdmin = "SystemAdmin";
}

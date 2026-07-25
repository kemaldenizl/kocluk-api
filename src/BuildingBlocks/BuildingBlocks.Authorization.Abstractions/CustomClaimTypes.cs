namespace BuildingBlocks.Authorization;

/// <summary>
/// Claim type names carried by the Security-issued access token. Shared so every service reads the
/// same claims without redefining the string constants.
/// </summary>
public static class CustomClaimTypes
{
    public const string Permission = "permission";
    public const string SessionId = "sid";
    public const string JwtId = "jti";
    public const string Subject = "sub";
    public const string Email = "email";
    public const string IssuedAt = "iat";

    /// <summary>JSON array of the user's active memberships: [{ tenantId, roles[], status }].</summary>
    public const string Memberships = "memberships";

    /// <summary><c>true</c> when the user holds the platform system-administrator role.</summary>
    public const string SystemAdmin = "system_admin";

    /// <summary>Name of a tenant-independent platform role held by the user (repeatable).</summary>
    public const string PlatformRole = "platform_role";
}

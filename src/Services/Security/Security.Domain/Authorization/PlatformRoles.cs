namespace Security.Domain.Authorization;

/// <summary>
/// Well-known platform-scoped roles. The system administrator role grants access to and management of
/// every tenant on the platform.
/// </summary>
public static class PlatformRoles
{
    public const string SystemAdmin = "SystemAdmin";
    public const string SystemAdminNormalized = "SYSTEMADMIN";
}

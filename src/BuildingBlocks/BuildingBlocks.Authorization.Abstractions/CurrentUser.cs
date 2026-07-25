namespace BuildingBlocks.Authorization;

/// <summary>
/// The authenticated caller projected from the access token claims. Framework-agnostic so any
/// service or application-layer handler can depend on it without an ASP.NET Core reference.
/// </summary>
public sealed record CurrentUser(
    Guid UserId,
    string Email,
    Guid? SessionId,
    string? AccessTokenJti,
    bool IsSystemAdmin,
    IReadOnlyCollection<string> Permissions
);

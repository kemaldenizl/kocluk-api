namespace Security.API.Common.Auth;

public sealed record CurrentUser(
    Guid UserId,
    string Email,
    Guid? SessionId,
    string? AccessTokenJti,
    bool IsSystemAdmin,
    IReadOnlyCollection<string> Permissions
);
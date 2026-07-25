using System.Security.Claims;
using BuildingBlocks.Authorization;

namespace BuildingBlocks.Authentication;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Projects an authenticated principal into a <see cref="CurrentUser"/>. Throws when the
    /// principal lacks a valid subject; call only on requests that passed authentication.
    /// </summary>
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(CustomClaimTypes.Subject);

        if (!Guid.TryParse(sub, out var userId))
            throw new InvalidOperationException("Authenticated user does not contain a valid subject identifier.");

        var email = principal.FindFirstValue(CustomClaimTypes.Email) ?? string.Empty;

        Guid? sessionId = null;
        var sid = principal.FindFirstValue(CustomClaimTypes.SessionId);
        if (Guid.TryParse(sid, out var parsedSessionId))
        {
            sessionId = parsedSessionId;
        }

        var jti = principal.FindFirstValue(CustomClaimTypes.JwtId);

        var isSystemAdmin = bool.TryParse(principal.FindFirstValue(CustomClaimTypes.SystemAdmin), out var parsed) && parsed;

        var permissions = principal.FindAll(CustomClaimTypes.Permission)
            .Select(x => x.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new CurrentUser(userId, email, sessionId, jti, isSystemAdmin, permissions);
    }
}

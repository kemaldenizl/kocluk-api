using System.Security.Claims;
using BuildingBlocks.Authorization;
using BuildingBlocks.Authorization.Validation;
using Security.Application.Abstractions.Security;

namespace Security.Infrastructure.Security;

/// <summary>
/// Post-signature access-token check for the Security service: rejects tokens that have been revoked
/// by <c>jti</c>, or invalidated for the whole user or a single session since they were issued. Wraps
/// the Redis-backed <see cref="IAccessTokenRevocationStore"/> and is plugged into the shared JWT
/// pipeline via <see cref="IAccessTokenValidator"/> (other services simply don't register it).
/// </summary>
public sealed class RedisAccessTokenValidator(IAccessTokenRevocationStore revocationStore)
    : IAccessTokenValidator
{
    public async Task<TokenValidationResult> ValidateAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var jti = principal.FindFirst(CustomClaimTypes.JwtId)?.Value;
        if (string.IsNullOrWhiteSpace(jti))
            return TokenValidationResult.Fail("Token does not contain a valid jti.");

        var sub = principal.FindFirst(CustomClaimTypes.Subject)?.Value;
        if (!Guid.TryParse(sub, out var userId))
            return TokenValidationResult.Fail("Token does not contain a valid subject.");

        var iatValue = principal.FindFirst(CustomClaimTypes.IssuedAt)?.Value;
        if (!long.TryParse(iatValue, out var issuedAtUnix))
            return TokenValidationResult.Fail("Token does not contain a valid iat.");

        var issuedAtUtc = DateTimeOffset.FromUnixTimeSeconds(issuedAtUnix).UtcDateTime;

        if (await revocationStore.IsRevokedAsync(jti, cancellationToken))
            return TokenValidationResult.Fail("Token has been revoked.");

        if (await revocationStore.IsUserTokenInvalidatedAsync(userId, issuedAtUtc, cancellationToken))
            return TokenValidationResult.Fail("User tokens have been invalidated.");

        var sid = principal.FindFirst(CustomClaimTypes.SessionId)?.Value;
        if (Guid.TryParse(sid, out var sessionId)
            && await revocationStore.IsSessionTokenInvalidatedAsync(sessionId, issuedAtUtc, cancellationToken))
        {
            return TokenValidationResult.Fail("Session tokens have been invalidated.");
        }

        return TokenValidationResult.Success();
    }
}

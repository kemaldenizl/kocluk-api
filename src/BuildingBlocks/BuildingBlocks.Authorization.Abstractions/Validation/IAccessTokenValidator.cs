using System.Security.Claims;

namespace BuildingBlocks.Authorization.Validation;

/// <summary>
/// Optional post-signature validation hook invoked after the JWT signature/issuer/audience/lifetime
/// checks pass. A service registers an implementation (e.g. a Redis-backed revocation check) to add
/// its own rejection rules; when none is registered, the shared pipeline performs no extra check.
/// </summary>
public interface IAccessTokenValidator
{
    Task<TokenValidationResult> ValidateAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}

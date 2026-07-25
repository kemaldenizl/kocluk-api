using Security.Application.Abstractions.Memberships;
using Security.Application.Auth.Dtos;

namespace Security.Application.Abstractions.Authentication;

public interface ITokenGenerator
{
    /// <summary>
    /// Issues an access token embedding the user's tenant memberships, platform roles and the union of
    /// their permissions (see <see cref="UserAccessContext"/>). No single tenant is pinned to the token.
    /// </summary>
    Task<AccessTokenDto> GenerateAccessTokenAsync(
        Guid userId,
        string email,
        UserAccessContext accessContext,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default);
}

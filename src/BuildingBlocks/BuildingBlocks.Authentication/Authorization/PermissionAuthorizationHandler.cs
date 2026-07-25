using BuildingBlocks.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace BuildingBlocks.Authentication;

/// <summary>
/// Succeeds when the caller's access token carries a <c>permission</c> claim matching the required
/// code (case-insensitive). The token embeds the flattened union of the user's permissions, so this
/// check is a pure claim lookup with no I/O.
/// </summary>
public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissions = context.User
            .FindAll(CustomClaimTypes.Permission)
            .Select(x => x.Value);

        if (permissions.Contains(requirement.Permission, StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

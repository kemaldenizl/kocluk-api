using System.Security.Claims;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace UnitTests.BuildingBlocks;

/// <summary>
/// The permission handler is the gate every protected endpoint runs through. It must succeed only when a
/// matching <c>permission</c> claim is present (case-insensitively) and stay silent otherwise so the
/// pipeline treats the requirement as unmet.
/// </summary>
public sealed class PermissionAuthorizationHandlerTests
{
    private static async Task<bool> Evaluate(string requiredPermission, params string[] granted)
    {
        var claims = granted.Select(p => new Claim(CustomClaimTypes.Permission, p));
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var requirement = new PermissionRequirement(requiredPermission);
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);
        return context.HasSucceeded;
    }

    [Fact]
    public async Task Succeeds_when_exact_permission_present()
        => (await Evaluate("users.read", "users.read", "roles.read")).ShouldBeTrue();

    [Fact]
    public async Task Succeeds_case_insensitively()
        => (await Evaluate("users.read", "USERS.READ")).ShouldBeTrue();

    [Fact]
    public async Task Fails_when_permission_absent()
        => (await Evaluate("tenants.manage", "users.read", "roles.read")).ShouldBeFalse();

    [Fact]
    public async Task Fails_when_no_permissions_at_all()
        => (await Evaluate("users.read")).ShouldBeFalse();
}

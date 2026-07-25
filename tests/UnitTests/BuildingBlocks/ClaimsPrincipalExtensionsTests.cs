using System.Security.Claims;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authorization;

namespace UnitTests.BuildingBlocks;

/// <summary>
/// <see cref="ClaimsPrincipalExtensions.ToCurrentUser"/> is the single translation from raw JWT claims to
/// the framework-agnostic <see cref="CurrentUser"/>. Every downstream authorization decision depends on it,
/// so each claim path — present, missing, malformed, duplicated — is exercised here.
/// </summary>
public sealed class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal PrincipalWith(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "test"));

    [Fact]
    public void Projects_all_claims_when_present()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var principal = PrincipalWith(
            new Claim(CustomClaimTypes.Subject, userId.ToString()),
            new Claim(CustomClaimTypes.Email, "ada@example.com"),
            new Claim(CustomClaimTypes.SessionId, sessionId.ToString()),
            new Claim(CustomClaimTypes.JwtId, "jti-123"),
            new Claim(CustomClaimTypes.SystemAdmin, "true"),
            new Claim(CustomClaimTypes.Permission, "users.read"),
            new Claim(CustomClaimTypes.Permission, "roles.read"));

        var current = principal.ToCurrentUser();

        current.UserId.ShouldBe(userId);
        current.Email.ShouldBe("ada@example.com");
        current.SessionId.ShouldBe(sessionId);
        current.AccessTokenJti.ShouldBe("jti-123");
        current.IsSystemAdmin.ShouldBeTrue();
        current.Permissions.ShouldBe(["users.read", "roles.read"], ignoreOrder: true);
    }

    [Fact]
    public void Throws_when_subject_is_missing()
    {
        var principal = PrincipalWith(new Claim(CustomClaimTypes.Email, "ada@example.com"));

        Should.Throw<InvalidOperationException>(() => principal.ToCurrentUser());
    }

    [Fact]
    public void Throws_when_subject_is_not_a_guid()
    {
        var principal = PrincipalWith(new Claim(CustomClaimTypes.Subject, "not-a-guid"));

        Should.Throw<InvalidOperationException>(() => principal.ToCurrentUser());
    }

    [Fact]
    public void Defaults_email_to_empty_when_absent()
    {
        var principal = PrincipalWith(new Claim(CustomClaimTypes.Subject, Guid.NewGuid().ToString()));

        principal.ToCurrentUser().Email.ShouldBe(string.Empty);
    }

    [Fact]
    public void Session_id_is_null_when_absent_or_malformed()
    {
        var withoutSid = PrincipalWith(new Claim(CustomClaimTypes.Subject, Guid.NewGuid().ToString()));
        var malformedSid = PrincipalWith(
            new Claim(CustomClaimTypes.Subject, Guid.NewGuid().ToString()),
            new Claim(CustomClaimTypes.SessionId, "xyz"));

        withoutSid.ToCurrentUser().SessionId.ShouldBeNull();
        malformedSid.ToCurrentUser().SessionId.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData("not-a-bool")]
    public void System_admin_is_false_unless_claim_is_true(string? value)
    {
        var claims = new List<Claim> { new(CustomClaimTypes.Subject, Guid.NewGuid().ToString()) };
        if (value is not null)
            claims.Add(new Claim(CustomClaimTypes.SystemAdmin, value));

        PrincipalWith([.. claims]).ToCurrentUser().IsSystemAdmin.ShouldBeFalse();
    }

    [Fact]
    public void Permissions_are_deduplicated_case_insensitively()
    {
        var principal = PrincipalWith(
            new Claim(CustomClaimTypes.Subject, Guid.NewGuid().ToString()),
            new Claim(CustomClaimTypes.Permission, "users.read"),
            new Claim(CustomClaimTypes.Permission, "USERS.READ"),
            new Claim(CustomClaimTypes.Permission, "roles.read"));

        principal.ToCurrentUser().Permissions.Count.ShouldBe(2);
    }

    [Fact]
    public void Permissions_are_empty_when_none_present()
    {
        var principal = PrincipalWith(new Claim(CustomClaimTypes.Subject, Guid.NewGuid().ToString()));

        principal.ToCurrentUser().Permissions.ShouldBeEmpty();
    }
}

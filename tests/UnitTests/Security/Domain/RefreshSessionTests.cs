using Security.Domain.Exceptions;
using Security.Domain.Sessions;

namespace UnitTests.Security.Domain;

public sealed class RefreshSessionTests
{
    private static RefreshToken TokenFor(RefreshSession session, string hash, DateTime? created = null, DateTime? expires = null)
        => new RefreshTokenBuilder()
            .WithSessionId(session.Id)
            .WithTokenHash(hash)
            .WithCreatedAt(created ?? TestClock.UtcNow)
            .WithExpiresAt(expires ?? TestClock.OneDayLater)
            .Build();

    [Fact]
    public void Constructor_rejects_invalid_arguments()
    {
        Should.Throw<DomainException>(() => new RefreshSession(Guid.Empty, Guid.NewGuid(), "d", "ip", TestClock.UtcNow));
        Should.Throw<DomainException>(() => new RefreshSession(Guid.NewGuid(), Guid.Empty, "d", "ip", TestClock.UtcNow));
        Should.Throw<DomainException>(() => new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), " ", "ip", TestClock.UtcNow));
        Should.Throw<DomainException>(() => new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), "d", " ", TestClock.UtcNow));
    }

    [Fact]
    public void AddToken_appends_token()
    {
        var session = RefreshSessionBuilder.Default();

        session.AddToken(TokenFor(session, "hash-1"));

        session.Tokens.ShouldHaveSingleItem();
    }

    [Fact]
    public void AddToken_null_throws()
        => Should.Throw<ArgumentNullException>(() => RefreshSessionBuilder.Default().AddToken(null!));

    [Fact]
    public void AddToken_to_revoked_session_throws()
    {
        var session = RefreshSessionBuilder.Default();
        session.Revoke(TestClock.UtcNow);

        Should.Throw<DomainException>(() => session.AddToken(TokenFor(session, "hash-1")));
    }

    [Fact]
    public void Revoke_revokes_session_and_all_active_tokens()
    {
        var session = RefreshSessionBuilder.Default();
        session.AddToken(TokenFor(session, "hash-1"));
        session.AddToken(TokenFor(session, "hash-2"));

        session.Revoke(TestClock.UtcNow);

        session.Revoked.ShouldBeTrue();
        session.RevokedAtUtc.ShouldBe(TestClock.UtcNow);
        session.Tokens.ShouldAllBe(t => t.Revoked);
    }

    [Fact]
    public void Revoke_is_idempotent()
    {
        var session = RefreshSessionBuilder.Default();
        session.Revoke(TestClock.UtcNow);

        session.Revoke(TestClock.OneHourLater);

        session.RevokedAtUtc.ShouldBe(TestClock.UtcNow); // first revoke time is preserved
    }

    [Fact]
    public void GetLatestActiveToken_returns_newest_usable()
    {
        var session = RefreshSessionBuilder.Default();
        session.AddToken(TokenFor(session, "old", created: TestClock.OneHourAgo));
        session.AddToken(TokenFor(session, "new", created: TestClock.UtcNow));

        session.GetLatestActiveToken(TestClock.UtcNow)!.TokenHash.ShouldBe("new");
    }

    [Fact]
    public void GetLatestActiveToken_ignores_consumed_and_expired()
    {
        var session = RefreshSessionBuilder.Default();
        var consumed = TokenFor(session, "consumed", created: TestClock.UtcNow);
        consumed.Consume(TestClock.UtcNow);
        session.AddToken(consumed);
        session.AddToken(TokenFor(session, "expired", created: TestClock.OneHourAgo, expires: TestClock.UtcNow));

        session.GetLatestActiveToken(TestClock.OneHourLater).ShouldBeNull();
    }

    [Fact]
    public void ContainsTokenHash_and_GetTokenByHash_work()
    {
        var session = RefreshSessionBuilder.Default();
        session.AddToken(TokenFor(session, "hash-1"));

        session.ContainsTokenHash("hash-1").ShouldBeTrue();
        session.ContainsTokenHash("missing").ShouldBeFalse();
        session.GetTokenByHash("hash-1")!.TokenHash.ShouldBe("hash-1");
        session.GetTokenByHash("missing").ShouldBeNull();
    }

    [Fact]
    public void Query_methods_reject_blank_hash()
    {
        var session = RefreshSessionBuilder.Default();

        Should.Throw<DomainException>(() => session.ContainsTokenHash(" "));
        Should.Throw<DomainException>(() => session.GetTokenByHash(" "));
    }
}

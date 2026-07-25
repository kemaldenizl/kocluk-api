using Security.Domain.Exceptions;
using Security.Domain.Sessions;

namespace UnitTests.Security.Domain;

public sealed class RefreshTokenTests
{
    [Fact]
    public void Constructor_requires_expiry_after_creation()
    {
        Should.Throw<DomainException>(() => new RefreshTokenBuilder()
            .WithCreatedAt(TestClock.UtcNow)
            .WithExpiresAt(TestClock.UtcNow)
            .Build());

        Should.Throw<DomainException>(() => new RefreshTokenBuilder()
            .WithCreatedAt(TestClock.UtcNow)
            .WithExpiresAt(TestClock.OneHourAgo)
            .Build());
    }

    [Fact]
    public void Constructor_rejects_empty_ids_and_blank_hash()
    {
        Should.Throw<DomainException>(() => new RefreshToken(Guid.Empty, Guid.NewGuid(), "h", TestClock.OneDayLater, TestClock.UtcNow));
        Should.Throw<DomainException>(() => new RefreshToken(Guid.NewGuid(), Guid.Empty, "h", TestClock.OneDayLater, TestClock.UtcNow));
        Should.Throw<DomainException>(() => new RefreshToken(Guid.NewGuid(), Guid.NewGuid(), " ", TestClock.OneDayLater, TestClock.UtcNow));
    }

    [Fact]
    public void Fresh_token_is_usable()
    {
        var token = RefreshTokenBuilder.Default();

        token.IsUsable(TestClock.UtcNow).ShouldBeTrue();
        token.IsExpired(TestClock.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void IsExpired_true_at_or_after_expiry()
    {
        var token = new RefreshTokenBuilder().WithExpiresAt(TestClock.OneHourLater).Build();

        token.IsExpired(TestClock.OneHourLater).ShouldBeTrue();
        token.IsExpired(TestClock.OneDayLater).ShouldBeTrue();
        token.IsUsable(TestClock.OneHourLater).ShouldBeFalse();
    }

    [Fact]
    public void Consume_marks_consumed_once()
    {
        var token = RefreshTokenBuilder.Default();

        token.Consume(TestClock.UtcNow);

        token.Consumed.ShouldBeTrue();
        token.ConsumedAtUtc.ShouldBe(TestClock.UtcNow);
        token.IsUsable(TestClock.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void Consume_twice_throws()
    {
        var token = RefreshTokenBuilder.Default();
        token.Consume(TestClock.UtcNow);

        Should.Throw<DomainException>(() => token.Consume(TestClock.OneHourLater));
    }

    [Fact]
    public void Consume_revoked_throws()
    {
        var token = RefreshTokenBuilder.Default();
        token.Revoke();

        Should.Throw<DomainException>(() => token.Consume(TestClock.UtcNow));
    }

    [Fact]
    public void Revoked_token_is_not_usable()
    {
        var token = RefreshTokenBuilder.Default();

        token.Revoke();

        token.Revoked.ShouldBeTrue();
        token.IsUsable(TestClock.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void Consume_rejects_default_time()
        => Should.Throw<DomainException>(() => RefreshTokenBuilder.Default().Consume(default));
}

using Security.Domain.Exceptions;
using Security.Domain.Tokens;

namespace UnitTests.Security.Domain;

/// <summary>
/// The four single-use, time-boxed tokens (email verification, password reset, password change, email
/// change) are structurally identical, so their whole lifecycle is verified once through a shared adapter.
/// This keeps the rules (expiry-after-creation, single use, usability window) in one place — DRY — while
/// still exercising every concrete type.
/// </summary>
public abstract class ExpiringTokenTestsBase
{
    protected interface ITokenProbe
    {
        void MarkUsed(DateTime utcNow);
        bool IsExpired(DateTime utcNow);
        bool IsUsable(DateTime utcNow);
        bool Used { get; }
        DateTime? UsedAtUtc { get; }
    }

    protected abstract ITokenProbe Create(Guid id, Guid userId, string hash, DateTime expiresAt, DateTime createdAt);

    private ITokenProbe Valid() => Create(Guid.NewGuid(), Guid.NewGuid(), "hash", TestClock.OneDayLater, TestClock.UtcNow);

    [Fact]
    public void Constructor_requires_expiry_after_creation()
    {
        Should.Throw<DomainException>(() => Create(Guid.NewGuid(), Guid.NewGuid(), "h", TestClock.UtcNow, TestClock.UtcNow));
        Should.Throw<DomainException>(() => Create(Guid.NewGuid(), Guid.NewGuid(), "h", TestClock.OneHourAgo, TestClock.UtcNow));
    }

    [Fact]
    public void Constructor_rejects_empty_ids_and_blank_hash()
    {
        Should.Throw<DomainException>(() => Create(Guid.Empty, Guid.NewGuid(), "h", TestClock.OneDayLater, TestClock.UtcNow));
        Should.Throw<DomainException>(() => Create(Guid.NewGuid(), Guid.Empty, "h", TestClock.OneDayLater, TestClock.UtcNow));
        Should.Throw<DomainException>(() => Create(Guid.NewGuid(), Guid.NewGuid(), " ", TestClock.OneDayLater, TestClock.UtcNow));
    }

    [Fact]
    public void Fresh_token_is_usable_and_not_expired()
    {
        var token = Valid();

        token.IsUsable(TestClock.UtcNow).ShouldBeTrue();
        token.IsExpired(TestClock.UtcNow).ShouldBeFalse();
        token.Used.ShouldBeFalse();
    }

    [Fact]
    public void IsExpired_true_at_or_after_expiry()
    {
        var token = Create(Guid.NewGuid(), Guid.NewGuid(), "h", TestClock.OneHourLater, TestClock.UtcNow);

        token.IsExpired(TestClock.OneHourLater).ShouldBeTrue();
        token.IsUsable(TestClock.OneHourLater).ShouldBeFalse();
    }

    [Fact]
    public void MarkUsed_makes_it_unusable()
    {
        var token = Valid();

        token.MarkUsed(TestClock.OneHourLater);

        token.Used.ShouldBeTrue();
        token.UsedAtUtc.ShouldBe(TestClock.OneHourLater);
        token.IsUsable(TestClock.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void MarkUsed_twice_throws()
    {
        var token = Valid();
        token.MarkUsed(TestClock.UtcNow);

        Should.Throw<DomainException>(() => token.MarkUsed(TestClock.OneHourLater));
    }

    [Fact]
    public void MarkUsed_and_queries_reject_default_time()
    {
        var token = Valid();

        Should.Throw<DomainException>(() => token.MarkUsed(default));
        Should.Throw<DomainException>(() => token.IsExpired(default));
    }
}

public sealed class EmailVerificationTokenTests : ExpiringTokenTestsBase
{
    private sealed class Probe(EmailVerificationToken token) : ITokenProbe
    {
        public void MarkUsed(DateTime utcNow) => token.MarkUsed(utcNow);
        public bool IsExpired(DateTime utcNow) => token.IsExpired(utcNow);
        public bool IsUsable(DateTime utcNow) => token.IsUsable(utcNow);
        public bool Used => token.Used;
        public DateTime? UsedAtUtc => token.UsedAtUtc;
    }

    protected override ITokenProbe Create(Guid id, Guid userId, string hash, DateTime expiresAt, DateTime createdAt)
        => new Probe(new EmailVerificationToken(id, userId, hash, expiresAt, createdAt));
}

public sealed class PasswordResetTokenTests : ExpiringTokenTestsBase
{
    private sealed class Probe(PasswordResetToken token) : ITokenProbe
    {
        public void MarkUsed(DateTime utcNow) => token.MarkUsed(utcNow);
        public bool IsExpired(DateTime utcNow) => token.IsExpired(utcNow);
        public bool IsUsable(DateTime utcNow) => token.IsUsable(utcNow);
        public bool Used => token.Used;
        public DateTime? UsedAtUtc => token.UsedAtUtc;
    }

    protected override ITokenProbe Create(Guid id, Guid userId, string hash, DateTime expiresAt, DateTime createdAt)
        => new Probe(new PasswordResetToken(id, userId, hash, expiresAt, createdAt));
}

public sealed class PasswordChangeTokenTests : ExpiringTokenTestsBase
{
    private sealed class Probe(PasswordChangeToken token) : ITokenProbe
    {
        public void MarkUsed(DateTime utcNow) => token.MarkUsed(utcNow);
        public bool IsExpired(DateTime utcNow) => token.IsExpired(utcNow);
        public bool IsUsable(DateTime utcNow) => token.IsUsable(utcNow);
        public bool Used => token.Used;
        public DateTime? UsedAtUtc => token.UsedAtUtc;
    }

    protected override ITokenProbe Create(Guid id, Guid userId, string hash, DateTime expiresAt, DateTime createdAt)
        => new Probe(new PasswordChangeToken(id, userId, hash, expiresAt, createdAt));
}

public sealed class EmailChangeTokenTests : ExpiringTokenTestsBase
{
    private sealed class Probe(EmailChangeToken token) : ITokenProbe
    {
        public void MarkUsed(DateTime utcNow) => token.MarkUsed(utcNow);
        public bool IsExpired(DateTime utcNow) => token.IsExpired(utcNow);
        public bool IsUsable(DateTime utcNow) => token.IsUsable(utcNow);
        public bool Used => token.Used;
        public DateTime? UsedAtUtc => token.UsedAtUtc;
    }

    protected override ITokenProbe Create(Guid id, Guid userId, string hash, DateTime expiresAt, DateTime createdAt)
        => new Probe(new EmailChangeToken(id, userId, hash, expiresAt, createdAt));
}

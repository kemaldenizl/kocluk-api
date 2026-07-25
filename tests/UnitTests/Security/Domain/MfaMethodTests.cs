using Security.Domain.Exceptions;
using Security.Domain.Mfa;

namespace UnitTests.Security.Domain;

public sealed class MfaMethodTests
{
    private static RecoveryCode Code(MfaMethod method, string hash)
        => new(Guid.NewGuid(), method.Id, hash, TestClock.UtcNow);

    [Fact]
    public void Constructor_starts_unverified_and_disabled()
    {
        var method = MfaMethodBuilder.Default();

        method.IsVerified.ShouldBeFalse();
        method.IsEnabled.ShouldBeFalse();
        method.RecoveryCodes.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_rejects_invalid_arguments()
    {
        Should.Throw<DomainException>(() => new MfaMethod(Guid.Empty, Guid.NewGuid(), MfaMethodType.Totp, "h", "e", TestClock.UtcNow));
        Should.Throw<DomainException>(() => new MfaMethod(Guid.NewGuid(), Guid.Empty, MfaMethodType.Totp, "h", "e", TestClock.UtcNow));
        Should.Throw<DomainException>(() => new MfaMethod(Guid.NewGuid(), Guid.NewGuid(), MfaMethodType.Totp, " ", "e", TestClock.UtcNow));
        Should.Throw<DomainException>(() => new MfaMethod(Guid.NewGuid(), Guid.NewGuid(), MfaMethodType.Totp, "h", " ", TestClock.UtcNow));
    }

    [Fact]
    public void VerifyAndEnable_flips_flags()
    {
        var method = MfaMethodBuilder.Default();

        method.VerifyAndEnable(TestClock.UtcNow);

        method.IsVerified.ShouldBeTrue();
        method.IsEnabled.ShouldBeTrue();
        method.VerifiedAtUtc.ShouldBe(TestClock.UtcNow);
        method.DisabledAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Disable_after_enable_sets_disabled()
    {
        var method = MfaMethodBuilder.Default();
        method.VerifyAndEnable(TestClock.UtcNow);

        method.Disable(TestClock.OneHourLater);

        method.IsEnabled.ShouldBeFalse();
        method.DisabledAtUtc.ShouldBe(TestClock.OneHourLater);
        method.IsVerified.ShouldBeTrue(); // remains verified
    }

    [Fact]
    public void VerifyAndEnable_and_Disable_reject_default_time()
    {
        var method = MfaMethodBuilder.Default();

        Should.Throw<DomainException>(() => method.VerifyAndEnable(default));
        Should.Throw<DomainException>(() => method.Disable(default));
    }

    [Fact]
    public void AddRecoveryCode_appends()
    {
        var method = MfaMethodBuilder.Default();

        method.AddRecoveryCode(Code(method, "hash-1"));

        method.RecoveryCodes.ShouldHaveSingleItem();
    }

    [Fact]
    public void AddRecoveryCode_null_throws()
        => Should.Throw<ArgumentNullException>(() => MfaMethodBuilder.Default().AddRecoveryCode(null!));

    [Fact]
    public void GetUsableRecoveryCodeByHash_returns_unused_match_only()
    {
        var method = MfaMethodBuilder.Default();
        var used = Code(method, "used");
        used.MarkUsed(TestClock.UtcNow);
        method.AddRecoveryCode(used);
        method.AddRecoveryCode(Code(method, "fresh"));

        method.GetUsableRecoveryCodeByHash("fresh").ShouldNotBeNull();
        method.GetUsableRecoveryCodeByHash("used").ShouldBeNull();
        method.GetUsableRecoveryCodeByHash("missing").ShouldBeNull();
    }

    [Fact]
    public void GetUsableRecoveryCodeByHash_rejects_blank()
        => Should.Throw<DomainException>(() => MfaMethodBuilder.Default().GetUsableRecoveryCodeByHash(" "));

    [Fact]
    public void ResetPendingSecret_replaces_secret_and_clears_state()
    {
        var method = MfaMethodBuilder.Default();
        method.VerifyAndEnable(TestClock.UtcNow);
        method.AddRecoveryCode(Code(method, "hash-1"));

        method.ResetPendingSecret("new-hash", "new-enc");

        method.SecretHash.ShouldBe("new-hash");
        method.SecretEncrypted.ShouldBe("new-enc");
        method.IsVerified.ShouldBeFalse();
        method.IsEnabled.ShouldBeFalse();
        method.RecoveryCodes.ShouldBeEmpty();
    }

    [Fact]
    public void ResetPendingSecret_rejects_blank()
    {
        var method = MfaMethodBuilder.Default();

        Should.Throw<DomainException>(() => method.ResetPendingSecret(" ", "e"));
        Should.Throw<DomainException>(() => method.ResetPendingSecret("h", " "));
    }

    [Fact]
    public void ClearRecoveryCodes_empties_collection()
    {
        var method = MfaMethodBuilder.Default();
        method.AddRecoveryCode(Code(method, "hash-1"));

        method.ClearRecoveryCodes();

        method.RecoveryCodes.ShouldBeEmpty();
    }
}

public sealed class RecoveryCodeTests
{
    [Fact]
    public void Constructor_rejects_invalid_arguments()
    {
        Should.Throw<DomainException>(() => new RecoveryCode(Guid.Empty, Guid.NewGuid(), "h", TestClock.UtcNow));
        Should.Throw<DomainException>(() => new RecoveryCode(Guid.NewGuid(), Guid.Empty, "h", TestClock.UtcNow));
        Should.Throw<DomainException>(() => new RecoveryCode(Guid.NewGuid(), Guid.NewGuid(), " ", TestClock.UtcNow));
        Should.Throw<DomainException>(() => new RecoveryCode(Guid.NewGuid(), Guid.NewGuid(), "h", default));
    }

    [Fact]
    public void MarkUsed_sets_used_once()
    {
        var code = new RecoveryCode(Guid.NewGuid(), Guid.NewGuid(), "h", TestClock.UtcNow);

        code.MarkUsed(TestClock.OneHourLater);

        code.Used.ShouldBeTrue();
        code.UsedAtUtc.ShouldBe(TestClock.OneHourLater);
    }

    [Fact]
    public void MarkUsed_twice_throws()
    {
        var code = new RecoveryCode(Guid.NewGuid(), Guid.NewGuid(), "h", TestClock.UtcNow);
        code.MarkUsed(TestClock.UtcNow);

        Should.Throw<DomainException>(() => code.MarkUsed(TestClock.OneHourLater));
    }

    [Fact]
    public void MarkUsed_rejects_default_time()
        => Should.Throw<DomainException>(() => new RecoveryCode(Guid.NewGuid(), Guid.NewGuid(), "h", TestClock.UtcNow).MarkUsed(default));
}

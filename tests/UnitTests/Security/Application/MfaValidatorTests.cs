using FluentValidation.TestHelper;
using Security.Application.Auth.Mfa.CompleteSetup;
using Security.Application.Auth.Mfa.Disable;
using Security.Application.Auth.Mfa.RecoveryCodes;
using Security.Application.Auth.Mfa.VerifyLogin;

namespace UnitTests.Security.Application;

public sealed class CompleteMfaSetupValidatorTests
{
    private readonly CompleteMfaSetupCommandValidator _validator = new();

    [Theory]
    [InlineData("123456")]
    [InlineData("12345678")]
    public void Accepts_6_to_8_digit_code(string code)
        => _validator.TestValidate(new CompleteMfaSetupCommand(Guid.NewGuid(), code)).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]         // empty
    [InlineData("12345")]    // too short
    [InlineData("123456789")]// too long
    [InlineData("12a456")]   // non-digit
    public void Rejects_bad_code(string code)
        => _validator.TestValidate(new CompleteMfaSetupCommand(Guid.NewGuid(), code)).ShouldHaveValidationErrorFor(x => x.Code);

    [Fact]
    public void Rejects_empty_user()
        => _validator.TestValidate(new CompleteMfaSetupCommand(Guid.Empty, "123456")).ShouldHaveValidationErrorFor(x => x.UserId);
}

public sealed class RegenerateRecoveryCodesValidatorTests
{
    private readonly RegenerateRecoveryCodesCommandValidator _validator = new();

    [Fact]
    public void Valid_passes()
        => _validator.TestValidate(new RegenerateRecoveryCodesCommand(Guid.NewGuid(), "123456")).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("abcdef")]
    public void Rejects_bad_totp(string code)
        => _validator.TestValidate(new RegenerateRecoveryCodesCommand(Guid.NewGuid(), code)).ShouldHaveValidationErrorFor(x => x.TotpCode);
}

public sealed class DisableMfaValidatorTests
{
    private readonly DisableMfaCommandValidator _validator = new();

    [Fact]
    public void Accepts_totp_only()
        => _validator.TestValidate(new DisableMfaCommand(Guid.NewGuid(), "123456", null)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Accepts_recovery_code_only()
        => _validator.TestValidate(new DisableMfaCommand(Guid.NewGuid(), null, "abcd1234")).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Rejects_when_neither_provided()
        => _validator.TestValidate(new DisableMfaCommand(Guid.NewGuid(), null, null))
            .ShouldHaveValidationErrorFor(x => x);

    [Theory]
    [InlineData("123")]       // too short
    [InlineData("123456789")] // too long
    [InlineData("12ab56")]    // non-digit
    public void Rejects_malformed_totp(string totp)
        => _validator.TestValidate(new DisableMfaCommand(Guid.NewGuid(), totp, null))
            .ShouldHaveValidationErrorFor(x => x.TotpCode);

    [Theory]
    [InlineData("short")]                 // < 8
    public void Rejects_too_short_recovery_code(string code)
        => _validator.TestValidate(new DisableMfaCommand(Guid.NewGuid(), null, code))
            .ShouldHaveValidationErrorFor(x => x.RecoveryCode);

    [Fact]
    public void Rejects_recovery_code_over_64_chars()
        => _validator.TestValidate(new DisableMfaCommand(Guid.NewGuid(), null, new string('a', 65)))
            .ShouldHaveValidationErrorFor(x => x.RecoveryCode);

    [Fact]
    public void Rejects_empty_user()
        => _validator.TestValidate(new DisableMfaCommand(Guid.Empty, "123456", null))
            .ShouldHaveValidationErrorFor(x => x.UserId);
}

public sealed class CompleteMfaLoginValidatorTests
{
    private readonly CompleteMfaLoginCommandValidator _validator = new();

    [Fact]
    public void Accepts_challenge_with_totp()
        => _validator.TestValidate(new CompleteMfaLoginCommand("challenge", "123456", null)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Accepts_challenge_with_recovery_code()
        => _validator.TestValidate(new CompleteMfaLoginCommand("challenge", null, "recovery1")).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Rejects_empty_challenge()
        => _validator.TestValidate(new CompleteMfaLoginCommand("", "123456", null))
            .ShouldHaveValidationErrorFor(x => x.ChallengeToken);

    [Fact]
    public void Rejects_when_neither_code_provided()
        => _validator.TestValidate(new CompleteMfaLoginCommand("challenge", null, null))
            .ShouldHaveValidationErrorFor(x => x);
}

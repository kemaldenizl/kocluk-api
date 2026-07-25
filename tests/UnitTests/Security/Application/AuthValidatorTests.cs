using FluentValidation.TestHelper;
using Security.Application.Auth.EmailChange.ConfirmEmailChange;
using Security.Application.Auth.EmailChange.RequestEmailChange;
using Security.Application.Auth.EmailChange.ValidateEmailChange;
using Security.Application.Auth.EmailVerification.ResendVerification;
using Security.Application.Auth.EmailVerification.VerifyEmail;
using Security.Application.Auth.Login;
using Security.Application.Auth.Logout;
using Security.Application.Auth.PasswordChange.ConfirmPasswordChange;
using Security.Application.Auth.PasswordChange.RequestPasswordChange;
using Security.Application.Auth.PasswordReset.ForgotPassword;
using Security.Application.Auth.PasswordReset.ResetPassword;
using Security.Application.Auth.Refresh;
using Security.Application.Auth.Register;
using Security.Domain.Users;

namespace UnitTests.Security.Application;

public sealed class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();
    private const string ValidPassword = "DummyPw123!xyz";

    private static RegisterCommand Valid() => new(
        "ada@example.com", ValidPassword, ValidPassword, "Ada", "Lovelace", StudentType.University);

    [Fact]
    public void Valid_command_passes() => _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Rejects_bad_email(string email)
        => _validator.TestValidate(Valid() with { Email = email }).ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void Rejects_email_over_320_chars()
        => _validator.TestValidate(Valid() with { Email = new string('a', 320) + "@x.com" })
            .ShouldHaveValidationErrorFor(x => x.Email);

    [Theory]
    [InlineData("Short1!")]              // too short
    [InlineData("alllowercase123!")]     // no uppercase
    [InlineData("ALLUPPERCASE123!")]     // no lowercase
    [InlineData("NoDigitsHere!!!")]      // no digit
    [InlineData("NoSpecialChars123")]    // no non-alphanumeric
    public void Rejects_weak_password(string password)
        => _validator.TestValidate(Valid() with { Password = password, ConfirmPassword = password })
            .ShouldHaveValidationErrorFor(x => x.Password);

    [Fact]
    public void Rejects_password_over_200_chars()
    {
        var pw = "Aa1!" + new string('a', 200);
        _validator.TestValidate(Valid() with { Password = pw, ConfirmPassword = pw })
            .ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Rejects_mismatched_confirmation()
        => _validator.TestValidate(Valid() with { ConfirmPassword = "Different123!xyz" })
            .ShouldHaveValidationErrorFor(x => x.ConfirmPassword);

    [Fact]
    public void Rejects_blank_names()
    {
        _validator.TestValidate(Valid() with { FirstName = "" }).ShouldHaveValidationErrorFor(x => x.FirstName);
        _validator.TestValidate(Valid() with { LastName = "" }).ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Rejects_names_over_100_chars()
        => _validator.TestValidate(Valid() with { FirstName = new string('a', 101) })
            .ShouldHaveValidationErrorFor(x => x.FirstName);

    [Fact]
    public void Rejects_undefined_student_type()
        => _validator.TestValidate(Valid() with { StudentType = (StudentType)999 })
            .ShouldHaveValidationErrorFor(x => x.StudentType);
}

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Valid_passes()
        => _validator.TestValidate(new LoginCommand("ada@example.com", "pw")).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("nope")]
    public void Rejects_bad_email(string email)
        => _validator.TestValidate(new LoginCommand(email, "pw")).ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void Rejects_empty_password()
        => _validator.TestValidate(new LoginCommand("ada@example.com", "")).ShouldHaveValidationErrorFor(x => x.Password);

    [Fact]
    public void Rejects_password_over_200_chars()
        => _validator.TestValidate(new LoginCommand("ada@example.com", new string('a', 201)))
            .ShouldHaveValidationErrorFor(x => x.Password);
}

public sealed class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Valid_passes()
        => _validator.TestValidate(new RefreshTokenCommand("token")).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Rejects_empty()
        => _validator.TestValidate(new RefreshTokenCommand("")).ShouldHaveValidationErrorFor(x => x.RefreshToken);

    [Fact]
    public void Rejects_over_2048_chars()
        => _validator.TestValidate(new RefreshTokenCommand(new string('a', 2049)))
            .ShouldHaveValidationErrorFor(x => x.RefreshToken);
}

public sealed class LogoutValidatorTests
{
    [Fact]
    public void Logout_valid_passes()
        => new LogoutCommandValidator()
            .TestValidate(new LogoutCommand(Guid.NewGuid(), Guid.NewGuid(), "jti", TestClock.UtcNow))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Logout_rejects_empty_ids_and_jti()
    {
        var result = new LogoutCommandValidator()
            .TestValidate(new LogoutCommand(Guid.Empty, Guid.Empty, "", TestClock.UtcNow));

        result.ShouldHaveValidationErrorFor(x => x.UserId);
        result.ShouldHaveValidationErrorFor(x => x.SessionId);
        result.ShouldHaveValidationErrorFor(x => x.AccessTokenJti);
    }

    [Fact]
    public void Logout_rejects_default_expiry()
        => new LogoutCommandValidator()
            .TestValidate(new LogoutCommand(Guid.NewGuid(), Guid.NewGuid(), "jti", default))
            .ShouldHaveValidationErrorFor(x => x.AccessTokenExpiresAtUtc);

    [Fact]
    public void LogoutAll_valid_passes()
        => new LogoutAllCommandValidator()
            .TestValidate(new LogoutAllCommand(Guid.NewGuid(), "jti", TestClock.UtcNow))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void LogoutAll_rejects_jti_over_200_chars()
        => new LogoutAllCommandValidator()
            .TestValidate(new LogoutAllCommand(Guid.NewGuid(), new string('a', 201), TestClock.UtcNow))
            .ShouldHaveValidationErrorFor(x => x.AccessTokenJti);
}

public sealed class PasswordResetValidatorTests
{
    private const string ValidPassword = "DummyPw123!xyz";

    [Fact]
    public void Forgot_valid_passes()
        => new ForgotPasswordCommandValidator().TestValidate(new ForgotPasswordCommand("ada@example.com"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Forgot_rejects_bad_email()
        => new ForgotPasswordCommandValidator().TestValidate(new ForgotPasswordCommand("nope"))
            .ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void Reset_valid_passes()
        => new ResetPasswordCommandValidator()
            .TestValidate(new ResetPasswordCommand("token", ValidPassword, ValidPassword))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Reset_rejects_weak_password_and_mismatch()
    {
        var validator = new ResetPasswordCommandValidator();

        validator.TestValidate(new ResetPasswordCommand("token", "weak", "weak"))
            .ShouldHaveValidationErrorFor(x => x.NewPassword);
        validator.TestValidate(new ResetPasswordCommand("token", ValidPassword, "Mismatch123!xyz"))
            .ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword);
    }

    [Fact]
    public void Reset_rejects_empty_token()
        => new ResetPasswordCommandValidator()
            .TestValidate(new ResetPasswordCommand("", ValidPassword, ValidPassword))
            .ShouldHaveValidationErrorFor(x => x.Token);
}

public sealed class PasswordChangeValidatorTests
{
    private const string ValidPassword = "DummyPw123!xyz";

    [Fact]
    public void Request_valid_passes()
        => new RequestPasswordChangeCommandValidator()
            .TestValidate(new RequestPasswordChangeCommand(Guid.NewGuid(), "current"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Request_rejects_empty_user_and_password()
    {
        var result = new RequestPasswordChangeCommandValidator()
            .TestValidate(new RequestPasswordChangeCommand(Guid.Empty, ""));

        result.ShouldHaveValidationErrorFor(x => x.UserId);
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Fact]
    public void Confirm_valid_passes()
        => new ConfirmPasswordChangeCommandValidator()
            .TestValidate(new ConfirmPasswordChangeCommand("token", ValidPassword, ValidPassword))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Confirm_rejects_mismatch()
        => new ConfirmPasswordChangeCommandValidator()
            .TestValidate(new ConfirmPasswordChangeCommand("token", ValidPassword, "Other123!xyz"))
            .ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword);
}

public sealed class EmailChangeAndVerificationValidatorTests
{
    [Fact]
    public void RequestEmailChange_valid_passes()
        => new RequestEmailChangeCommandValidator()
            .TestValidate(new RequestEmailChangeCommand(Guid.NewGuid(), "current"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ConfirmEmailChange_valid_passes()
        => new ConfirmEmailChangeCommandValidator()
            .TestValidate(new ConfirmEmailChangeCommand("token", "new@example.com"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ConfirmEmailChange_rejects_bad_email()
        => new ConfirmEmailChangeCommandValidator()
            .TestValidate(new ConfirmEmailChangeCommand("token", "nope"))
            .ShouldHaveValidationErrorFor(x => x.NewEmail);

    [Fact]
    public void ValidateEmailChange_rejects_empty_token()
        => new ValidateEmailChangeQueryValidator().TestValidate(new ValidateEmailChangeQuery(""))
            .ShouldHaveValidationErrorFor(x => x.Token);

    [Fact]
    public void VerifyEmail_rejects_empty_token()
        => new VerifyEmailCommandValidator().TestValidate(new VerifyEmailCommand(""))
            .ShouldHaveValidationErrorFor(x => x.Token);

    [Fact]
    public void ResendVerification_rejects_bad_email()
        => new ResendVerificationCommandValidator().TestValidate(new ResendVerificationCommand("nope"))
            .ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void ResendVerification_valid_passes()
        => new ResendVerificationCommandValidator().TestValidate(new ResendVerificationCommand("ada@example.com"))
            .ShouldNotHaveAnyValidationErrors();
}

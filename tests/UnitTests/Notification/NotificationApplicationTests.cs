using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using Notification.Application.Emails;
using Notification.Application.Notifications.QueueNotification;
using Notification.Domain.Notifications;

namespace UnitTests.Notification;

public sealed class QueueNotificationValidatorTests
{
    private readonly QueueNotificationCommandValidator _validator = new();

    private static QueueNotificationCommand Valid() => new(
        Guid.NewGuid(), NotificationType.EmailVerification, "ada@example.com", "token", TestClock.UtcNow, "idem-1");

    [Fact]
    public void Valid_passes() => _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Rejects_bad_recipient(string recipient)
        => _validator.TestValidate(Valid() with { Recipient = recipient }).ShouldHaveValidationErrorFor(x => x.Recipient);

    [Fact]
    public void Rejects_empty_token()
        => _validator.TestValidate(Valid() with { Token = "" }).ShouldHaveValidationErrorFor(x => x.Token);

    [Fact]
    public void Rejects_empty_idempotency_key()
        => _validator.TestValidate(Valid() with { IdempotencyKey = "" }).ShouldHaveValidationErrorFor(x => x.IdempotencyKey);

    [Fact]
    public void Rejects_idempotency_key_over_256_chars()
        => _validator.TestValidate(Valid() with { IdempotencyKey = new string('a', 257) })
            .ShouldHaveValidationErrorFor(x => x.IdempotencyKey);

    [Fact]
    public void Rejects_undefined_type()
        => _validator.TestValidate(Valid() with { Type = (NotificationType)999 }).ShouldHaveValidationErrorFor(x => x.Type);
}

public sealed class EmailTemplateFactoryTests
{
    private static EmailTemplateFactory Factory() => new(Options.Create(new EmailLinkOptions
    {
        VerifyEmailUrl = "https://app.test/verify",
        ResetPasswordUrl = "https://app.test/reset",
        ConfirmPasswordChangeUrl = "https://app.test/confirm-password",
        ConfirmEmailChangeUrl = "https://app.test/confirm-email"
    }));

    [Theory]
    [InlineData(NotificationType.EmailVerification, "Verify your email address", "https://app.test/verify")]
    [InlineData(NotificationType.PasswordReset, "Reset your password", "https://app.test/reset")]
    [InlineData(NotificationType.PasswordChange, "Confirm your password change", "https://app.test/confirm-password")]
    [InlineData(NotificationType.EmailChange, "Confirm your new email address", "https://app.test/confirm-email")]
    public void Build_uses_correct_subject_and_base_url(NotificationType type, string subject, string baseUrl)
    {
        var template = Factory().Build(type, "ada@example.com", "tok");

        template.Subject.ShouldBe(subject);
        template.HtmlBody.ShouldContain(baseUrl);
    }

    [Fact]
    public void Build_url_encodes_token()
    {
        var template = Factory().Build(NotificationType.EmailVerification, "ada@example.com", "a b/c&d");

        template.HtmlBody.ShouldContain("token=a%20b%2Fc%26d");
    }

    [Fact]
    public void Build_embeds_the_action_link_twice_button_and_fallback()
    {
        var template = Factory().Build(NotificationType.PasswordReset, "ada@example.com", "tok");

        Regex_Count(template.HtmlBody, "https://app.test/reset?token=tok").ShouldBe(2);
    }

    [Fact]
    public void Build_throws_for_unsupported_type()
        => Should.Throw<ArgumentOutOfRangeException>(() => Factory().Build((NotificationType)999, "ada@example.com", "tok"));

    private static int Regex_Count(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}

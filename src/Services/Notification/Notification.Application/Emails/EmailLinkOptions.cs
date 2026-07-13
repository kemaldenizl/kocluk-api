namespace Notification.Application.Emails;

/// <summary>
/// Base URLs of the client-facing pages that action links point to. The relevant token is appended as a
/// query-string parameter when a template is rendered.
/// </summary>
public sealed class EmailLinkOptions
{
    public const string SectionName = "EmailLinks";

    public string VerifyEmailUrl { get; init; } = "https://app.local/verify-email";

    public string ResetPasswordUrl { get; init; } = "https://app.local/reset-password";

    public string ConfirmPasswordChangeUrl { get; init; } = "https://app.local/confirm-password-change";

    public string ConfirmEmailChangeUrl { get; init; } = "https://app.local/confirm-email-change";
}

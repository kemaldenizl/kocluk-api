using Microsoft.Extensions.Options;
using Notification.Domain.Notifications;

namespace Notification.Application.Emails;

/// <summary>
/// Builds the subject and HTML body for each notification type. Action links are assembled from the
/// configured base URLs (<see cref="EmailLinkOptions"/>) with the token appended as a query parameter.
/// </summary>
public sealed class EmailTemplateFactory(IOptions<EmailLinkOptions> options) : IEmailTemplateFactory
{
    private readonly EmailLinkOptions _links = options.Value;

    public EmailTemplate Build(NotificationType type, string recipient, string token)
    {
        return type switch
        {
            NotificationType.EmailVerification => new EmailTemplate(
                "Verify your email address",
                ActionEmail(
                    "Confirm your email",
                    "Thanks for signing up. Please confirm your email address to activate your account.",
                    "Verify email",
                    Link(_links.VerifyEmailUrl, token))),

            NotificationType.PasswordReset => new EmailTemplate(
                "Reset your password",
                ActionEmail(
                    "Reset your password",
                    "We received a request to reset your password. If this was you, continue below. This link will expire shortly.",
                    "Reset password",
                    Link(_links.ResetPasswordUrl, token))),

            NotificationType.PasswordChange => new EmailTemplate(
                "Confirm your password change",
                ActionEmail(
                    "Confirm your password change",
                    "Please confirm that you want to change your account password.",
                    "Confirm change",
                    Link(_links.ConfirmPasswordChangeUrl, token))),

            NotificationType.EmailChange => new EmailTemplate(
                "Confirm your new email address",
                ActionEmail(
                    "Confirm your new email",
                    "Please confirm that you want to use this address for your account.",
                    "Confirm email",
                    Link(_links.ConfirmEmailChangeUrl, token))),

            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported notification type.")
        };
    }

    private static string Link(string baseUrl, string token) =>
        $"{baseUrl}?token={Uri.EscapeDataString(token)}";

    private static string ActionEmail(string heading, string body, string buttonText, string url) =>
        $$"""
        <!doctype html>
        <html>
          <body style="font-family: Arial, Helvetica, sans-serif; color: #1a1a1a; line-height: 1.5;">
            <h2 style="margin: 0 0 16px;">{{heading}}</h2>
            <p style="margin: 0 0 24px;">{{body}}</p>
            <p style="margin: 0 0 24px;">
              <a href="{{url}}" style="background: #2563eb; color: #ffffff; padding: 12px 20px; border-radius: 6px; text-decoration: none; display: inline-block;">{{buttonText}}</a>
            </p>
            <p style="margin: 0 0 8px; color: #6b7280; font-size: 13px;">If the button does not work, copy and paste this link into your browser:</p>
            <p style="margin: 0; color: #6b7280; font-size: 13px; word-break: break-all;">{{url}}</p>
          </body>
        </html>
        """;
}

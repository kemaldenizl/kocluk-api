namespace Notification.Domain.Notifications;

/// <summary>
/// The kind of transactional email a <see cref="Notification"/> represents. Drives which template the
/// application renders. Persisted as a string for readability and forward compatibility.
/// </summary>
public enum NotificationType
{
    EmailVerification = 1,
    PasswordReset = 2,
    PasswordChange = 3,
    EmailChange = 4
}

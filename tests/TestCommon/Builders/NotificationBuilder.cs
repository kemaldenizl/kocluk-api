using Notification.Domain.Notifications;
using DomainNotification = Notification.Domain.Notifications.Notification;

namespace TestCommon.Builders;

/// <summary>Fluent builder for the Notification outbox aggregate, starting from a valid Pending row.</summary>
public sealed class NotificationBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _tenantId = Guid.NewGuid();
    private string _recipient = "ada@example.com";
    private NotificationType _type = NotificationType.EmailVerification;
    private string _subject = "Verify your email";
    private string _htmlBody = "<html><body>Hello</body></html>";
    private string _idempotencyKey = "idem-key-1";
    private DateTime _createdAtUtc = TestClock.UtcNow;

    public NotificationBuilder WithId(Guid id) { _id = id; return this; }
    public NotificationBuilder WithTenantId(Guid tenantId) { _tenantId = tenantId; return this; }
    public NotificationBuilder WithRecipient(string recipient) { _recipient = recipient; return this; }
    public NotificationBuilder WithType(NotificationType type) { _type = type; return this; }
    public NotificationBuilder WithSubject(string subject) { _subject = subject; return this; }
    public NotificationBuilder WithHtmlBody(string htmlBody) { _htmlBody = htmlBody; return this; }
    public NotificationBuilder WithIdempotencyKey(string idempotencyKey) { _idempotencyKey = idempotencyKey; return this; }
    public NotificationBuilder WithCreatedAt(DateTime createdAtUtc) { _createdAtUtc = createdAtUtc; return this; }

    public DomainNotification Build() => new(
        _id, _tenantId, _recipient, _type, _subject, _htmlBody, _idempotencyKey, _createdAtUtc);

    public static DomainNotification Default() => new NotificationBuilder().Build();
}

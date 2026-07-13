using BuildingBlocks.Domain;
using Notification.Domain.Common;

namespace Notification.Domain.Notifications;

/// <summary>
/// A single transactional email captured as a durable, store-and-forward outbox row. The row is created
/// <see cref="NotificationStatus.Pending"/> the moment an integration event is consumed, decoupling the
/// external provider call from the message transaction. A background dispatcher then delivers it and moves
/// it to <see cref="NotificationStatus.Sent"/> or, after exhausting retries, <see cref="NotificationStatus.DeadLettered"/>.
/// <para><see cref="IdempotencyKey"/> is unique: replays of the same logical request never create a second mail.</para>
/// </summary>
public sealed class Notification : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Recipient { get; private set; } = default!;
    public NotificationType Type { get; private set; }
    public string Subject { get; private set; } = default!;
    public string HtmlBody { get; private set; } = default!;
    public string IdempotencyKey { get; private set; } = default!;
    public NotificationStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime NextAttemptAtUtc { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }

    private Notification()
    {
    }

    public Notification(
        Guid id,
        Guid tenantId,
        string recipient,
        NotificationType type,
        string subject,
        string htmlBody,
        string idempotencyKey,
        DateTime createdAtUtc)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        TenantId = Guard.AgainstEmpty(tenantId, nameof(tenantId));
        Recipient = Guard.AgainstNullOrWhiteSpace(recipient, nameof(recipient));
        Subject = Guard.AgainstNullOrWhiteSpace(subject, nameof(subject));
        HtmlBody = Guard.AgainstNullOrWhiteSpace(htmlBody, nameof(htmlBody));
        IdempotencyKey = Guard.AgainstNullOrWhiteSpace(idempotencyKey, nameof(idempotencyKey));
        CreatedAtUtc = Guard.AgainstDefault(createdAtUtc, nameof(createdAtUtc));
        Type = type;

        Status = NotificationStatus.Pending;
        AttemptCount = 0;
        NextAttemptAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    /// <summary>Marks a delivery attempt as successful and records the provider's message id.</summary>
    public void MarkSent(string providerMessageId, DateTime at)
    {
        AttemptCount++;
        Status = NotificationStatus.Sent;
        ProviderMessageId = providerMessageId;
        LastError = null;
        SentAtUtc = at;
        UpdatedAtUtc = at;
    }

    /// <summary>Records a failed attempt that will be retried at <paramref name="nextAttemptAtUtc"/>.</summary>
    public void MarkFailed(string error, DateTime nextAttemptAtUtc, DateTime at)
    {
        AttemptCount++;
        Status = NotificationStatus.Failed;
        LastError = Truncate(error);
        NextAttemptAtUtc = nextAttemptAtUtc;
        UpdatedAtUtc = at;
    }

    /// <summary>Records a terminal failure after retries are exhausted; the row is parked for inspection.</summary>
    public void MarkDeadLettered(string error, DateTime at)
    {
        AttemptCount++;
        Status = NotificationStatus.DeadLettered;
        LastError = Truncate(error);
        UpdatedAtUtc = at;
    }

    private static string Truncate(string value)
    {
        const int maxLength = 2000;
        value = value.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}

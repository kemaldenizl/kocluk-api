namespace Notification.Infrastructure.Dispatch;

public sealed class NotificationDispatcherOptions
{
    public const string SectionName = "NotificationDispatcher";

    /// <summary>How often the dispatcher polls for due notifications.</summary>
    public int PollingIntervalSeconds { get; init; } = 10;

    /// <summary>Maximum notifications processed per poll.</summary>
    public int BatchSize { get; init; } = 20;

    /// <summary>Delivery attempts before a notification is dead-lettered.</summary>
    public int MaxAttempts { get; init; } = 5;

    /// <summary>Base delay for the exponential retry backoff (delay = base * 2^(attempt-1)).</summary>
    public int BaseBackoffSeconds { get; init; } = 30;
}

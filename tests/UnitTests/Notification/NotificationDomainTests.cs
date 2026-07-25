using Notification.Domain.Exceptions;
using Notification.Domain.Notifications;

namespace UnitTests.Notification;

public sealed class NotificationDomainTests
{
    [Fact]
    public void Constructor_starts_pending_with_zero_attempts()
    {
        var notification = new NotificationBuilder().WithCreatedAt(TestClock.UtcNow).Build();

        notification.Status.ShouldBe(NotificationStatus.Pending);
        notification.AttemptCount.ShouldBe(0);
        notification.NextAttemptAtUtc.ShouldBe(TestClock.UtcNow);
        notification.UpdatedAtUtc.ShouldBe(TestClock.UtcNow);
        notification.SentAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Constructor_rejects_invalid_arguments()
    {
        Should.Throw<DomainException>(() => new NotificationBuilder().WithId(Guid.Empty).Build());
        Should.Throw<DomainException>(() => new NotificationBuilder().WithTenantId(Guid.Empty).Build());
        Should.Throw<DomainException>(() => new NotificationBuilder().WithRecipient(" ").Build());
        Should.Throw<DomainException>(() => new NotificationBuilder().WithSubject(" ").Build());
        Should.Throw<DomainException>(() => new NotificationBuilder().WithHtmlBody(" ").Build());
        Should.Throw<DomainException>(() => new NotificationBuilder().WithIdempotencyKey(" ").Build());
        Should.Throw<DomainException>(() => new NotificationBuilder().WithCreatedAt(default).Build());
    }

    [Fact]
    public void MarkSent_completes_delivery()
    {
        var notification = NotificationBuilder.Default();

        notification.MarkSent("provider-id", TestClock.OneHourLater);

        notification.Status.ShouldBe(NotificationStatus.Sent);
        notification.AttemptCount.ShouldBe(1);
        notification.ProviderMessageId.ShouldBe("provider-id");
        notification.LastError.ShouldBeNull();
        notification.SentAtUtc.ShouldBe(TestClock.OneHourLater);
        notification.UpdatedAtUtc.ShouldBe(TestClock.OneHourLater);
    }

    [Fact]
    public void MarkSent_clears_previous_error()
    {
        var notification = NotificationBuilder.Default();
        notification.MarkFailed("boom", TestClock.OneHourLater, TestClock.OneHourLater);

        notification.MarkSent("provider-id", TestClock.OneDayLater);

        notification.LastError.ShouldBeNull();
        notification.AttemptCount.ShouldBe(2);
    }

    [Fact]
    public void MarkFailed_records_retry_state()
    {
        var notification = NotificationBuilder.Default();

        notification.MarkFailed("smtp timeout", TestClock.OneDayLater, TestClock.OneHourLater);

        notification.Status.ShouldBe(NotificationStatus.Failed);
        notification.AttemptCount.ShouldBe(1);
        notification.LastError.ShouldBe("smtp timeout");
        notification.NextAttemptAtUtc.ShouldBe(TestClock.OneDayLater);
        notification.UpdatedAtUtc.ShouldBe(TestClock.OneHourLater);
    }

    [Fact]
    public void MarkFailed_truncates_error_to_2000_chars()
    {
        var notification = NotificationBuilder.Default();
        var longError = new string('x', 5000);

        notification.MarkFailed(longError, TestClock.OneDayLater, TestClock.OneHourLater);

        notification.LastError!.Length.ShouldBe(2000);
    }

    [Fact]
    public void MarkFailed_trims_error()
    {
        var notification = NotificationBuilder.Default();

        notification.MarkFailed("   spaced   ", TestClock.OneDayLater, TestClock.OneHourLater);

        notification.LastError.ShouldBe("spaced");
    }

    [Fact]
    public void MarkDeadLettered_parks_the_row()
    {
        var notification = NotificationBuilder.Default();

        notification.MarkDeadLettered("permanent failure", TestClock.OneHourLater);

        notification.Status.ShouldBe(NotificationStatus.DeadLettered);
        notification.AttemptCount.ShouldBe(1);
        notification.LastError.ShouldBe("permanent failure");
        notification.UpdatedAtUtc.ShouldBe(TestClock.OneHourLater);
    }

    [Fact]
    public void Attempt_count_accumulates_across_failures()
    {
        var notification = NotificationBuilder.Default();

        notification.MarkFailed("e1", TestClock.OneHourLater, TestClock.OneHourLater);
        notification.MarkFailed("e2", TestClock.OneDayLater, TestClock.OneDayLater);
        notification.MarkDeadLettered("final", TestClock.OneDayLater);

        notification.AttemptCount.ShouldBe(3);
    }
}

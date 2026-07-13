using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Abstractions.Email;
using Notification.Application.Abstractions.Persistence;
using Notification.Application.Abstractions.Time;
using Notification.Application.Abstractions.UnitOfWork;
using DomainNotification = Notification.Domain.Notifications.Notification;

namespace Notification.Infrastructure.Dispatch;

/// <summary>
/// Store-and-forward dispatcher. Notifications are persisted <c>Pending</c> when their integration event is
/// consumed; this worker delivers them out-of-band, decoupling the external Resend call from the message
/// transaction. Failures are retried with exponential backoff and dead-lettered after
/// <see cref="NotificationDispatcherOptions.MaxAttempts"/> attempts.
/// </summary>
public sealed class NotificationDispatcherService(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationDispatcherOptions> options,
    ILogger<NotificationDispatcherService> logger) : BackgroundService
{
    private readonly NotificationDispatcherOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.PollingIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notification dispatch cycle failed; will retry next interval.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var provider = scope.ServiceProvider;

        var repository = provider.GetRequiredService<INotificationRepository>();
        var emailSender = provider.GetRequiredService<IEmailSender>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var clock = provider.GetRequiredService<IDateTimeProvider>();

        var batch = await repository.GetDispatchBatchAsync(clock.UtcNow, _options.BatchSize, cancellationToken);
        if (batch.Count == 0)
            return;

        foreach (var notification in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await DispatchOneAsync(notification, emailSender, clock, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchOneAsync(
        DomainNotification notification,
        IEmailSender emailSender,
        IDateTimeProvider clock,
        CancellationToken cancellationToken)
    {
        var message = new EmailMessage(
            notification.Recipient,
            notification.Subject,
            notification.HtmlBody,
            notification.IdempotencyKey);

        var result = await emailSender.SendAsync(message, cancellationToken);
        var now = clock.UtcNow;

        if (result.IsSuccess)
        {
            notification.MarkSent(result.ProviderMessageId!, now);
            return;
        }

        var error = result.Error ?? "Unknown delivery failure.";

        // AttemptCount is incremented by the Mark* methods, so this attempt is the (current + 1)-th.
        if (notification.AttemptCount + 1 >= _options.MaxAttempts)
        {
            notification.MarkDeadLettered(error, now);
            logger.LogError(
                "Notification {NotificationId} dead-lettered after {Attempts} attempts: {Error}",
                notification.Id, notification.AttemptCount + 1, error);
            return;
        }

        var backoff = TimeSpan.FromSeconds(_options.BaseBackoffSeconds * Math.Pow(2, notification.AttemptCount));
        notification.MarkFailed(error, now.Add(backoff), now);
    }
}

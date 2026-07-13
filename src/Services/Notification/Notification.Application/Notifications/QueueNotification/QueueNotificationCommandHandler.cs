using MediatR;
using Notification.Application.Abstractions.Persistence;
using Notification.Application.Abstractions.Time;
using Notification.Application.Abstractions.UnitOfWork;
using Notification.Application.Emails;
using DomainNotification = Notification.Domain.Notifications.Notification;

namespace Notification.Application.Notifications.QueueNotification;

public sealed class QueueNotificationCommandHandler(
    INotificationRepository notificationRepository,
    IEmailTemplateFactory templateFactory,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<QueueNotificationCommand>
{
    public async Task Handle(QueueNotificationCommand request, CancellationToken cancellationToken)
    {
        // Application-level idempotency: the same logical event may be redelivered or republished.
        // The unique index on IdempotencyKey is the backstop; this check avoids a failed insert on the hot path.
        if (await notificationRepository.ExistsByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken))
            return;

        var template = templateFactory.Build(request.Type, request.Recipient, request.Token);

        var notification = new DomainNotification(
            Guid.NewGuid(),
            request.TenantId,
            request.Recipient,
            request.Type,
            template.Subject,
            template.HtmlBody,
            request.IdempotencyKey,
            dateTimeProvider.UtcNow);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

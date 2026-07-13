using FluentValidation;

namespace Notification.Application.Notifications.QueueNotification;

public sealed class QueueNotificationCommandValidator : AbstractValidator<QueueNotificationCommand>
{
    public QueueNotificationCommandValidator()
    {
        RuleFor(x => x.Recipient)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Token)
            .NotEmpty();

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Type)
            .IsInEnum();
    }
}

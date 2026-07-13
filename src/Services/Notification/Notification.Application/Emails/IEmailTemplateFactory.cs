using Notification.Domain.Notifications;

namespace Notification.Application.Emails;

/// <summary>Renders the subject and HTML body for a given notification type and action token.</summary>
public interface IEmailTemplateFactory
{
    EmailTemplate Build(NotificationType type, string recipient, string token);
}

public sealed record EmailTemplate(string Subject, string HtmlBody);

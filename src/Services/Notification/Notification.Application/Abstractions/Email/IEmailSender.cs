namespace Notification.Application.Abstractions.Email;

/// <summary>
/// Abstraction over the transactional email provider (Resend). The <see cref="EmailMessage.IdempotencyKey"/>
/// is forwarded to the provider so an ambiguous retry never results in a duplicate delivery.
/// </summary>
public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public sealed record EmailMessage(string Recipient, string Subject, string HtmlBody, string IdempotencyKey);

public sealed record EmailSendResult(bool IsSuccess, string? ProviderMessageId, string? Error)
{
    public static EmailSendResult Success(string providerMessageId) => new(true, providerMessageId, null);

    public static EmailSendResult Failure(string error) => new(false, null, error);
}

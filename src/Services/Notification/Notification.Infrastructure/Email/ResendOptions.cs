namespace Notification.Infrastructure.Email;

public sealed class ResendOptions
{
    public const string SectionName = "Resend";

    public string ApiKey { get; init; } = default!;

    public string FromEmail { get; init; } = default!;

    public string? FromName { get; init; }

    public string BaseAddress { get; init; } = "https://api.resend.com";
}

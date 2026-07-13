using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Abstractions.Email;

namespace Notification.Infrastructure.Email;

/// <summary>
/// Sends transactional email through the Resend HTTP API. The notification's idempotency key is forwarded
/// as the provider <c>Idempotency-Key</c> header, so a retry after an ambiguous failure will not send twice.
/// Transport/HTTP failures are returned as <see cref="EmailSendResult.Failure"/> (never thrown) so the
/// dispatcher can apply its retry/backoff policy.
/// </summary>
public sealed class ResendEmailSender(
    HttpClient httpClient,
    IOptions<ResendOptions> options,
    ILogger<ResendEmailSender> logger) : IEmailSender
{
    private readonly ResendOptions _options = options.Value;

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var from = string.IsNullOrWhiteSpace(_options.FromName)
            ? _options.FromEmail
            : $"{_options.FromName} <{_options.FromEmail}>";

        var payload = new ResendSendRequest(from, [message.Recipient], message.Subject, message.HtmlBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/emails")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", message.IdempotencyKey);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<ResendSendResponse>(cancellationToken);

                if (body is null || string.IsNullOrWhiteSpace(body.Id))
                    return EmailSendResult.Failure("Resend returned a success status without a message id.");

                return EmailSendResult.Success(body.Id);
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "Resend rejected email to {Recipient}: {StatusCode} {Body}",
                message.Recipient, (int)response.StatusCode, error);

            return EmailSendResult.Failure($"Resend responded {(int)response.StatusCode}: {Trim(error)}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Transport failure sending email to {Recipient} via Resend.", message.Recipient);
            return EmailSendResult.Failure($"Transport failure: {ex.Message}");
        }
    }

    private static string Trim(string value)
    {
        value = value.Trim();
        return value.Length <= 500 ? value : value[..500];
    }

    private sealed record ResendSendRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] IReadOnlyList<string> To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html);

    private sealed record ResendSendResponse(
        [property: JsonPropertyName("id")] string? Id);
}

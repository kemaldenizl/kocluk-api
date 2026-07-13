using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Notification.API.HealthChecks;

/// <summary>Writes a compact JSON health report (overall status plus per-check status and duration).</summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = entry.Value.Duration.TotalMilliseconds
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions));
    }
}

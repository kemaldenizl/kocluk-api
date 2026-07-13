using Notification.Application.Abstractions.Time;

namespace Notification.Infrastructure.Time;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

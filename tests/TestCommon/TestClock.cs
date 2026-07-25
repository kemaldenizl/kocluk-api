namespace TestCommon;

/// <summary>
/// Deterministic time anchors for tests. Using fixed, well-known instants keeps assertions stable and
/// makes "expiry", "before/after" and "touch" scenarios explicit instead of relying on wall-clock time.
/// </summary>
public static class TestClock
{
    /// <summary>A fixed "now" every test can build around.</summary>
    public static readonly DateTime UtcNow = new(2026, 07, 25, 12, 00, 00, DateTimeKind.Utc);

    public static readonly DateTime OneHourAgo = UtcNow.AddHours(-1);
    public static readonly DateTime OneHourLater = UtcNow.AddHours(1);
    public static readonly DateTime OneDayLater = UtcNow.AddDays(1);
}

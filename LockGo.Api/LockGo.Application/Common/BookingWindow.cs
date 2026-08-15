namespace LockGo.Application.Common;

/// <summary>
/// The span a booking would occupy. Availability everywhere — search, detail,
/// and the booking write path — is an overlap test against this window, so
/// browsing a future slot and booking it agree on what "free" means.
/// A zero-length window means "free right now", the walk-up default.
/// </summary>
public readonly record struct BookingWindow(DateTimeOffset Start, DateTimeOffset End)
{
    public static BookingWindow From(DateTimeOffset? start, int? durationHours)
    {
        var windowStart = start ?? DateTimeOffset.UtcNow;
        return new BookingWindow(windowStart, windowStart.AddHours(durationHours ?? 0));
    }
}

namespace WindLordApi.Worker.Schedulers;

/// <summary>
/// Helper class for calculating clock-aligned scheduled run times.
/// </summary>
public static class ClockAlignedSchedulerHelper
{
    /// <summary>
    /// Calculates the next scheduled run time based on clock-aligned intervals.
    /// For example, with a 15-minute interval and 15-second offset:
    /// - If current time is 10:07:30, next run is 10:15:15
    /// - If current time is 10:15:00, next run is 10:15:15
    /// - If current time is 10:15:20, next run is 10:30:15
    /// </summary>
    /// <param name="interval">The interval between runs (e.g., 15 minutes)</param>
    /// <param name="offset">The delay after each interval mark (e.g., 15 seconds)</param>
    /// <returns>The next scheduled DateTimeOffset when the job should run</returns>
    public static DateTimeOffset CalculateNextScheduledRunTime(TimeSpan interval, TimeSpan offset)
    {
        var now = DateTimeOffset.UtcNow;
        var intervalMinutes = (int)interval.TotalMinutes;

        // Round down to the current interval mark (e.g., 10:15:20 -> 10:15:00)
        var currentIntervalMinute = (now.Minute / intervalMinutes) * intervalMinutes;
        var currentIntervalTime = new DateTimeOffset(
            now.Year, now.Month, now.Day,
            now.Hour, currentIntervalMinute, 0, now.Offset);

        // Calculate the scheduled time for the current interval (add offset)
        var scheduledTimeForCurrentInterval = currentIntervalTime.Add(offset);

        // If the scheduled time for the current interval has already passed, use the next interval
        if (scheduledTimeForCurrentInterval <= now)
        {
            return currentIntervalTime.Add(interval).Add(offset);
        }

        return scheduledTimeForCurrentInterval;
    }
}


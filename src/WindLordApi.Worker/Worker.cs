using WindLordApi.Worker.Services;
using WindLordApi.Worker.Startup;

namespace WindLordApi.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Worker(
        ILogger<Worker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run all jobs once on startup
        await StartupJobs.RunStartupJobsAsync(_serviceProvider, _logger, stoppingToken);

        // Create periodic timers for scheduled jobs
        // Each timer must be unique - PeriodicTimer only supports a single concurrent consumer
        var metFrostObservationDataInterval = new PeriodicTimer(TimeSpan.FromMinutes(5));
        var metFrostNewStationsInterval = new PeriodicTimer(TimeSpan.FromDays(7)); // For SyncNewWeatherStationsAsync
        var metFrostStationsActiveStatusInterval = new PeriodicTimer(TimeSpan.FromDays(7)); // For SyncWeatherStationActiveStatusAsync

        // Start all scheduled jobs concurrently
        var syncDataTask = RunPeriodicJobAsync(
            metFrostObservationDataInterval,
            async (service, ct) => await service.SyncLatestStationDataAsync(ct),
            "SyncLatestStationDataAsync",
            stoppingToken);

        var syncStationsTask = RunPeriodicJobAsync(
            metFrostNewStationsInterval,
            async (service, ct) => await service.SyncWeatherStationsAsync(ct),
            "SyncNewWeatherStationsAsync",
            stoppingToken);

        var syncStatusTask = RunPeriodicJobAsync(
            metFrostStationsActiveStatusInterval,
            async (service, ct) => await service.SyncWeatherStationsActiveStatusAsync(ct),
            "SyncWeatherStationActiveStatusAsync",
            stoppingToken);

        var holfuySyncTask = RunHolfuySyncAtClockIntervalsAsync(
            TimeSpan.FromMinutes(15),
            TimeSpan.FromSeconds(15),
            stoppingToken);

        // Wait for all tasks (they will run until cancellation is requested)
        await Task.WhenAll(syncDataTask, syncStationsTask, syncStatusTask, holfuySyncTask);
    }

    private async Task RunPeriodicJobAsync(
        PeriodicTimer timer,
        Func<IMetFrostSyncService, CancellationToken, Task> jobAction,
        string jobName,
        CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait for the next timer tick
                var hasNextTick = await timer.WaitForNextTickAsync(stoppingToken);
                if (!hasNextTick)
                {
                    break; // Timer was disposed
                }

                _logger.LogInformation("Starting scheduled job: {JobName}", jobName);

                try
                {
                    // Create a new scope for each job execution
                    using var scope = _serviceProvider.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<IMetFrostSyncService>();
                    await jobAction(syncService, stoppingToken);
                    _logger.LogInformation("Completed scheduled job: {JobName}", jobName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in scheduled job: {JobName}", jobName);
                    // Continue to next iteration - don't stop the timer
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scheduled job {JobName} was cancelled", jobName);
        }
        finally
        {
            timer.Dispose();
        }
    }

    /// <summary>
    /// Runs the Holfuy sync job at clock-aligned intervals (e.g., :00, :15, :30, :45) 
    /// with a specified offset delay after each interval mark.
    /// </summary>
    /// <param name="interval">The interval between runs (e.g., 15 minutes)</param>
    /// <param name="offset">The delay after each interval mark (e.g., 15 seconds)</param>
    /// <param name="stoppingToken">Cancellation token</param>
    private async Task RunHolfuySyncAtClockIntervalsAsync(
        TimeSpan interval,
        TimeSpan offset,
        CancellationToken stoppingToken)
    {
        const string jobName = "SyncHolfuyDataAsync";

        try
        {
            // Calculate and wait until the next scheduled run time
            var nextRunTime = CalculateNextScheduledRunTime(interval, offset);
            var delayUntilNextRun = nextRunTime - DateTimeOffset.UtcNow;

            if (delayUntilNextRun > TimeSpan.Zero)
            {
                _logger.LogInformation(
                    "Job {JobName} will start at {ScheduledTime} (in {DelaySeconds:F1} seconds)",
                    jobName,
                    nextRunTime.ToString("HH:mm:ss"),
                    delayUntilNextRun.TotalSeconds);

                await Task.Delay(delayUntilNextRun, stoppingToken);
            }

            // Create periodic timer that will tick every interval (15 minutes)
            using var timer = new PeriodicTimer(interval);

            // Run the job immediately (we're now at a scheduled time)
            await ExecuteHolfuyJobOnceAsync(jobName, stoppingToken);

            // Continue with periodic execution every interval
            while (!stoppingToken.IsCancellationRequested)
            {
                var hasNextTick = await timer.WaitForNextTickAsync(stoppingToken);
                if (!hasNextTick)
                {
                    break; // Timer was disposed
                }

                await ExecuteHolfuyJobOnceAsync(jobName, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scheduled job {JobName} was cancelled", jobName);
        }
    }

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
    private DateTimeOffset CalculateNextScheduledRunTime(TimeSpan interval, TimeSpan offset)
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

    private async Task ExecuteHolfuyJobOnceAsync(
        string jobName,
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting scheduled job: {JobName}", jobName);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IHolfuySyncService>();
            var count = await syncService.SyncHolfuyDataAsync(stoppingToken);
            _logger.LogInformation("Completed scheduled job: {JobName} (inserted {Count} new records)", jobName, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scheduled job: {JobName}", jobName);
            // Continue to next iteration - don't stop the timer
        }
    }
}

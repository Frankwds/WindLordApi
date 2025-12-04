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

    private async Task RunHolfuySyncAtClockIntervalsAsync(
        TimeSpan interval,
        TimeSpan offset,
        CancellationToken stoppingToken)
    {
        const string jobName = "SyncHolfuyDataAsync";

        try
        {
            // Calculate delay until next scheduled time (15 seconds past next 15-minute mark)
            var now = DateTimeOffset.UtcNow;
            var intervalMinutes = (int)interval.TotalMinutes;
            var minutes = now.Minute;

            // Calculate minutes until next interval mark
            var minutesToNext = intervalMinutes - (minutes % intervalMinutes);

            // If exactly on the mark, wait for the next interval
            if (minutesToNext == intervalMinutes)
            {
                minutesToNext = 0;
            }

            var nextRun = now.AddMinutes(minutesToNext);
            // Round down to the exact minute and add the offset (15 seconds)
            nextRun = new DateTimeOffset(
                nextRun.Year, nextRun.Month, nextRun.Day,
                nextRun.Hour, nextRun.Minute, 0, nextRun.Offset)
                .Add(offset);

            var delay = nextRun - now;

            if (delay.TotalMilliseconds > 0)
            {
                _logger.LogInformation(
                    "Job {JobName} will start at {ScheduledTime} (in {DelaySeconds:F1} seconds)",
                    jobName,
                    nextRun.ToString("HH:mm:ss"),
                    delay.TotalSeconds);

                await Task.Delay(delay, stoppingToken);
            }

            // Now create the periodic timer - it will tick every 15 minutes from this point
            using var timer = new PeriodicTimer(interval);

            // Run the job immediately (we're now at a scheduled time)
            await ExecuteHolfuyJobOnceAsync(jobName, stoppingToken);

            // Continue with periodic execution
            while (!stoppingToken.IsCancellationRequested)
            {
                var hasNextTick = await timer.WaitForNextTickAsync(stoppingToken);
                if (!hasNextTick)
                {
                    break;
                }

                await ExecuteHolfuyJobOnceAsync(jobName, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scheduled job {JobName} was cancelled", jobName);
        }
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

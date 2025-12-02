using WindLordApi.Worker.Services;

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
        await RunStartupJobsAsync(stoppingToken);

        // Create periodic timers for scheduled jobs
        // Each timer must be unique - PeriodicTimer only supports a single concurrent consumer
        var fiveMinuteTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        var twoWeekTimer = new PeriodicTimer(TimeSpan.FromDays(14)); // For SyncNewWeatherStationsAsync
        var weeklyTimer = new PeriodicTimer(TimeSpan.FromDays(7)); // For SyncWeatherStationActiveStatusAsync

        // Start all scheduled jobs concurrently
        var syncDataTask = RunPeriodicJobAsync(
            fiveMinuteTimer,
            async (service, ct) => await service.SyncLatestStationDataAsync(ct),
            "SyncLatestStationDataAsync",
            stoppingToken);

        var syncStationsTask = RunPeriodicJobAsync(
            twoWeekTimer,
            async (service, ct) => await service.SyncWeatherStationsAsync(ct),
            "SyncNewWeatherStationsAsync",
            stoppingToken);

        var syncStatusTask = RunPeriodicJobAsync(
            weeklyTimer,
            async (service, ct) => await service.SyncWeatherStationsActiveStatusAsync(ct),
            "SyncWeatherStationActiveStatusAsync",
            stoppingToken);

        // Wait for all tasks (they will run until cancellation is requested)
        await Task.WhenAll(syncDataTask, syncStationsTask, syncStatusTask);
    }

    private async Task RunStartupJobsAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Running startup jobs...");

        // Sync all stations on startup
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IMetFrostSyncService>();
            await syncService.SyncLatestStationDataAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running SyncLatestStationDataAsync on startup");
        }

        // Sync new weather stations on startup
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IMetFrostSyncService>();
            await syncService.SyncWeatherStationsAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running SyncNewWeatherStationsAsync on startup");
        }

        // Sync weather station active status on startup
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IMetFrostSyncService>();
            await syncService.SyncWeatherStationsActiveStatusAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running SyncWeatherStationActiveStatusAsync on startup");
        }

        _logger.LogInformation("Startup jobs completed");
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
}

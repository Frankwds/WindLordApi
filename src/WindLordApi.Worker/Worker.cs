using WindLordApi.Worker.Schedulers;
using WindLordApi.Worker.Startup;

namespace WindLordApi.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly PeriodicJobScheduler _periodicJobScheduler;
    private readonly ClockAlignedScheduler _clockAlignedScheduler;

    public Worker(
        ILogger<Worker> logger,
        IServiceProvider serviceProvider,
        PeriodicJobScheduler periodicJobScheduler,
        ClockAlignedScheduler clockAlignedScheduler)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _periodicJobScheduler = periodicJobScheduler;
        _clockAlignedScheduler = clockAlignedScheduler;
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
        var syncDataTask = _periodicJobScheduler.RunAsync(
            metFrostObservationDataInterval,
            async (service, ct) => await service.SyncLatestStationDataAsync(ct),
            "SyncLatestStationDataAsync",
            stoppingToken);

        var syncStationsTask = _periodicJobScheduler.RunAsync(
            metFrostNewStationsInterval,
            async (service, ct) => await service.SyncWeatherStationsAsync(ct),
            "SyncNewWeatherStationsAsync",
            stoppingToken);

        var syncStatusTask = _periodicJobScheduler.RunAsync(
            metFrostStationsActiveStatusInterval,
            async (service, ct) => await service.SyncWeatherStationsActiveStatusAsync(ct),
            "SyncWeatherStationActiveStatusAsync",
            stoppingToken);

        var holfuySyncTask = _clockAlignedScheduler.RunAsync(
            TimeSpan.FromMinutes(15),
            TimeSpan.FromSeconds(30),
            "SyncHolfuyDataAsync",
            async (service, ct) => await service.SyncHolfuyDataAsync(ct),
            stoppingToken);

        // Wait for all tasks (they will run until cancellation is requested)
        await Task.WhenAll(syncDataTask, syncStationsTask, syncStatusTask, holfuySyncTask);
    }
}

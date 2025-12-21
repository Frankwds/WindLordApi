using WindLordApi.Worker.Schedulers;
using WindLordApi.Worker.Services;
using WindLordApi.Worker.Startup;

namespace WindLordApi.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly PeriodicJobScheduler<IMetFrostSyncService> _periodicJobScheduler;
    private readonly ClockAlignedScheduler<IHolfuySyncService> _clockAlignedScheduler;
    private readonly PeriodicJobScheduler<IForecastUpdateService> _forecastUpdateScheduler;

    public Worker(
        ILogger<Worker> logger,
        IServiceProvider serviceProvider,
        PeriodicJobScheduler<IMetFrostSyncService> periodicJobScheduler,
        ClockAlignedScheduler<IHolfuySyncService> clockAlignedScheduler,
        PeriodicJobScheduler<IForecastUpdateService> forecastUpdateScheduler)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _periodicJobScheduler = periodicJobScheduler;
        _clockAlignedScheduler = clockAlignedScheduler;
        _forecastUpdateScheduler = forecastUpdateScheduler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run all jobs once on startup
        // await StartupJobs.RunStartupJobsAsync(_serviceProvider, _logger, stoppingToken);

        // Track schedule for visualization
        var scheduleStartTime = DateTime.UtcNow;
        var jobSchedule = new List<(string Name, TimeSpan InitialDelay, TimeSpan Interval, DateTime FirstRun)>();
        var currentDelay = TimeSpan.Zero;

        // Define job intervals
        var metFrostObservationInterval = TimeSpan.FromMinutes(5);
        var metFrostNewStationsInterval = TimeSpan.FromDays(7);
        var metFrostActiveStatusInterval = TimeSpan.FromDays(7);
        var forecastUpdateInterval = TimeSpan.FromMinutes(5);
        var holfuySyncInterval = TimeSpan.FromMinutes(15);


        var metFrostObservationDataTimer = new PeriodicTimer(metFrostObservationInterval);
        jobSchedule.Add(("SyncLatestStationDataAsync", currentDelay, metFrostObservationInterval, scheduleStartTime.Add(metFrostObservationInterval)));
        var syncDataTask = _periodicJobScheduler.RunAsync(
            metFrostObservationDataTimer,
            async (service, ct) => { await service.SyncLatestStationDataAsync(ct); },
            "SyncLatestStationDataAsync",
            stoppingToken);

        var metFrostNewStationsTimer = new PeriodicTimer(metFrostNewStationsInterval);
        jobSchedule.Add(("SyncNewWeatherStationsAsync", currentDelay, metFrostNewStationsInterval, scheduleStartTime.Add(metFrostNewStationsInterval)));
        var syncStationsTask = _periodicJobScheduler.RunAsync(
            metFrostNewStationsTimer,
            async (service, ct) => { await service.SyncWeatherStationsAsync(ct); },
            "SyncNewWeatherStationsAsync",
            stoppingToken);

        var holfuySyncTask = _clockAlignedScheduler.RunAsync(
            TimeSpan.FromMinutes(15),
            TimeSpan.FromSeconds(30),
            "SyncHolfuyDataAsync",
            async (service, ct) => { await service.SyncHolfuyDataAsync(ct); },
            stoppingToken);

        // Stagger initialization of the following jobs to avoid overlapping with other jobs running at same interval
        currentDelay += TimeSpan.FromSeconds(15);
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        var forecastUpdateTimer = new PeriodicTimer(forecastUpdateInterval);
        jobSchedule.Add(("UpdateForecastsAsync", currentDelay, forecastUpdateInterval, scheduleStartTime.Add(forecastUpdateInterval + currentDelay)));
        var forecastUpdateTask = _forecastUpdateScheduler.RunAsync(
            forecastUpdateTimer,
            async (service, ct) => { await service.UpdateForecastsAsync(ct); },
            "UpdateForecastsAsync",
            stoppingToken);

        var metFrostActiveStatusTimer = new PeriodicTimer(metFrostActiveStatusInterval);
        jobSchedule.Add(("SyncWeatherStationActiveStatusAsync", currentDelay, metFrostActiveStatusInterval, scheduleStartTime.Add(metFrostActiveStatusInterval + currentDelay)));
        var syncStatusTask = _periodicJobScheduler.RunAsync(
            metFrostActiveStatusTimer,
            async (service, ct) => { await service.SyncWeatherStationsActiveStatusAsync(ct); },
            "SyncWeatherStationActiveStatusAsync",
            stoppingToken);

        // Print schedule visualization
        PrintJobSchedule(jobSchedule, scheduleStartTime);

        // Wait for all tasks (they will run until cancellation is requested)
        await Task.WhenAll(syncDataTask, syncStationsTask, syncStatusTask, holfuySyncTask, forecastUpdateTask);
    }

    private void PrintJobSchedule(List<(string Name, TimeSpan InitialDelay, TimeSpan Interval, DateTime FirstRun)> schedule, DateTime startTime)
    {
        _logger.LogInformation("═══════════════════════════════════════════════════════════════════════════");
        _logger.LogInformation("                          JOB SCHEDULE OVERVIEW                            ");
        _logger.LogInformation("═══════════════════════════════════════════════════════════════════════════");
        _logger.LogInformation("Schedule initialized at: {StartTime:yyyy-MM-dd HH:mm:ss} UTC", startTime);
        _logger.LogInformation("");
        _logger.LogInformation("{JobName,-40} {Delay,12} {Interval,15} {NextRun,20}",
            "Job Name", "Init Delay", "Interval", "First Run (UTC)");
        _logger.LogInformation("───────────────────────────────────────────────────────────────────────────");

        foreach (var job in schedule.OrderBy(j => j.FirstRun))
        {
            var delayStr = FormatTimeSpan(job.InitialDelay);
            var intervalStr = FormatTimeSpan(job.Interval);
            var nextRunStr = job.FirstRun.ToString("yyyy-MM-dd HH:mm:ss");

            _logger.LogInformation("{JobName,-40} {Delay,12} {Interval,15} {NextRun,20}",
                job.Name, delayStr, intervalStr, nextRunStr);
        }

        _logger.LogInformation("═══════════════════════════════════════════════════════════════════════════");
        _logger.LogInformation("");

    }

    private static string FormatTimeSpan(TimeSpan span)
    {
        if (span.TotalDays >= 1)
            return $"{span.TotalDays:F1}d";
        if (span.TotalHours >= 1)
            return $"{span.TotalHours:F1}h";
        if (span.TotalMinutes >= 1)
            return $"{span.TotalMinutes:F1}m";
        return $"{span.TotalSeconds:F0}s";
    }
}

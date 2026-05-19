using Serilog;
using WindLordApi.Worker.Schedulers;
using WindLordApi.Worker.Services;
using WindLordApi.Worker.Startup;

namespace WindLordApi.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly CronScheduler<IMetFrostSyncService> _metFrostScheduler;
    private readonly CronScheduler<IHolfuySyncService> _holfuyScheduler;
    private readonly CronScheduler<IForecastUpdateService> _forecastUpdateScheduler;
    private readonly CronScheduler<IWindsMobiSyncService> _windsMobiScheduler;
    private readonly CronScheduler<ICountryLocatorService> _countryLocatorScheduler;
    private readonly CronScheduler<IPortWindStationRefreshService> _portWindStationRefreshScheduler;
    private readonly CronScheduler<IPortWindObservationSyncService> _portWindObservationScheduler;

    public Worker(
        ILogger<Worker> logger,
        IServiceProvider serviceProvider,
        CronScheduler<IMetFrostSyncService> metFrostScheduler,
        CronScheduler<IHolfuySyncService> holfuyScheduler,
        CronScheduler<IForecastUpdateService> forecastUpdateScheduler,
        CronScheduler<IWindsMobiSyncService> windsMobiScheduler,
        CronScheduler<ICountryLocatorService> countryLocatorScheduler,
        CronScheduler<IPortWindStationRefreshService> portWindStationRefreshScheduler,
        CronScheduler<IPortWindObservationSyncService> portWindObservationScheduler)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _metFrostScheduler = metFrostScheduler;
        _holfuyScheduler = holfuyScheduler;
        _forecastUpdateScheduler = forecastUpdateScheduler;
        _windsMobiScheduler = windsMobiScheduler;
        _countryLocatorScheduler = countryLocatorScheduler;
        _portWindStationRefreshScheduler = portWindStationRefreshScheduler;
        _portWindObservationScheduler = portWindObservationScheduler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run all jobs once on startup
        await StartupJobs.RunStartupJobsAsync(_serviceProvider, _logger, stoppingToken);

        // Track schedule for visualization
        var scheduleStartTime = DateTime.UtcNow;
        var jobSchedule = new List<(string Name, string CronExpression, DateTime? FirstRun, int ExpectedDuration)>();

        // Define cron schedules with expected execution times
        var forecastUpdateCron = "0 1/5 * * * *";      // Every 5 min at :01:00 seconds (34s duration)
        var holfuyCron = "30 */15 * * * *";             // Every 15 min at :30 seconds (18s duration)
        var metFrostDataCron = "0 2/5 * * * *";         // Every 5 min at :02:00 (35s duration)
        var metFrostNewStationsCron = "0 0 3 * * SUN";  // Sundays at 3:00 AM (2s duration)
        var metFrostActiveStatusCron = "0 0 4 * * SUN"; // Sundays at 4:00 AM (2s duration)
        var windsMobiCron = "0 */5 * * * *";              // Every 5 min at :00 seconds (~30s duration)
        var countryLocatorCron = "0 0 5 * * SUN";          // Sundays at 5:00 AM
        var portWindObservationCron = "0 4/5 * * * *";     // Every 5 min starting at minute 4
        var portWindStationRefreshCron = "0 0 6 * * SUN";  // Sundays at 6:00 AM

        // Calculate next run times for all jobs
        var forecastUpdateNextRun = CronScheduler<IForecastUpdateService>.CalculateNextRunTime(forecastUpdateCron);
        var holfuyNextRun = CronScheduler<IHolfuySyncService>.CalculateNextRunTime(holfuyCron);
        var metFrostDataNextRun = CronScheduler<IMetFrostSyncService>.CalculateNextRunTime(metFrostDataCron);
        var metFrostNewStationsNextRun = CronScheduler<IMetFrostSyncService>.CalculateNextRunTime(metFrostNewStationsCron);
        var metFrostActiveStatusNextRun = CronScheduler<IMetFrostSyncService>.CalculateNextRunTime(metFrostActiveStatusCron);
        var windsMobiNextRun = CronScheduler<IWindsMobiSyncService>.CalculateNextRunTime(windsMobiCron);
        var countryLocatorNextRun = CronScheduler<ICountryLocatorService>.CalculateNextRunTime(countryLocatorCron);
        var portWindObservationNextRun = CronScheduler<IPortWindObservationSyncService>.CalculateNextRunTime(portWindObservationCron);
        var portWindStationRefreshNextRun = CronScheduler<IPortWindStationRefreshService>.CalculateNextRunTime(portWindStationRefreshCron);

        // Add jobs to schedule
        jobSchedule.Add(("UpdateForecastsAsync", forecastUpdateCron, forecastUpdateNextRun?.DateTime, 34));
        jobSchedule.Add(("SyncHolfuyDataAsync", holfuyCron, holfuyNextRun?.DateTime, 18));
        jobSchedule.Add(("SyncLatestStationDataAsync", metFrostDataCron, metFrostDataNextRun?.DateTime, 35));
        jobSchedule.Add(("SyncNewWeatherStationsAsync", metFrostNewStationsCron, metFrostNewStationsNextRun?.DateTime, 2));
        jobSchedule.Add(("SyncWeatherStationActiveStatusAsync", metFrostActiveStatusCron, metFrostActiveStatusNextRun?.DateTime, 2));
        jobSchedule.Add(("SyncWindsMobiDataAsync", windsMobiCron, windsMobiNextRun?.DateTime, 30));
        jobSchedule.Add(("LocateCountriesAsync", countryLocatorCron, countryLocatorNextRun?.DateTime, 10));
        jobSchedule.Add(("SyncPortWindLatestStationDataAsync", portWindObservationCron, portWindObservationNextRun?.DateTime, 30));
        jobSchedule.Add(("SyncPortWindWeatherStationsAsync", portWindStationRefreshCron, portWindStationRefreshNextRun?.DateTime, 10));

        // Print schedule visualization
        PrintJobSchedule(jobSchedule, scheduleStartTime);

        // Start all scheduled tasks
        var forecastUpdateTask = _forecastUpdateScheduler.RunAsync(
            forecastUpdateCron,
            async (service, ct) => { await service.UpdateForecastsAsync(ct); },
            "UpdateForecastsAsync",
            stoppingToken);

        var holfuySyncTask = _holfuyScheduler.RunAsync(
            holfuyCron,
            async (service, ct) => { await service.SyncHolfuyDataAsync(ct); },
            "SyncHolfuyDataAsync",
            stoppingToken);

        var syncDataTask = _metFrostScheduler.RunAsync(
            metFrostDataCron,
            async (service, ct) => { await service.SyncLatestStationDataAsync(ct); },
            "SyncLatestStationDataAsync",
            stoppingToken);

        var syncStationsTask = _metFrostScheduler.RunAsync(
            metFrostNewStationsCron,
            async (service, ct) => { await service.SyncWeatherStationsAsync(ct); },
            "SyncNewWeatherStationsAsync",
            stoppingToken);

        var syncStatusTask = _metFrostScheduler.RunAsync(
            metFrostActiveStatusCron,
            async (service, ct) => { await service.SyncWeatherStationsActiveStatusAsync(ct); },
            "SyncWeatherStationActiveStatusAsync",
            stoppingToken);

        var windsMobiSyncTask = _windsMobiScheduler.RunAsync(
            windsMobiCron,
            async (service, ct) => { await service.SyncWindsMobiDataAsync(ct); },
            "SyncWindsMobiDataAsync",
            stoppingToken);

        var countryLocatorTask = _countryLocatorScheduler.RunAsync(
            countryLocatorCron,
            async (service, ct) => { await service.LocateCountriesAsync(ct); },
            "LocateCountriesAsync",
            stoppingToken);

        var portWindObservationTask = _portWindObservationScheduler.RunAsync(
            portWindObservationCron,
            async (service, ct) => { await service.SyncLatestStationDataAsync(ct); },
            "SyncPortWindLatestStationDataAsync",
            stoppingToken);

        var portWindStationRefreshTask = _portWindStationRefreshScheduler.RunAsync(
            portWindStationRefreshCron,
            async (service, ct) => { await service.SyncWeatherStationsAsync(ct); },
            "SyncPortWindWeatherStationsAsync",
            stoppingToken);

        // Wait for all tasks (they will run until cancellation is requested)
        await Task.WhenAll(
            syncDataTask,
            syncStationsTask,
            syncStatusTask,
            holfuySyncTask,
            forecastUpdateTask,
            windsMobiSyncTask,
            countryLocatorTask,
            portWindObservationTask,
            portWindStationRefreshTask);
    }

    private void PrintJobSchedule(List<(string Name, string CronExpression, DateTime? FirstRun, int ExpectedDuration)> schedule, DateTime startTime)
    {
        _logger.LogInformation("═══════════════════════════════════════════════════════════════════════════");
        _logger.LogInformation("                          JOB SCHEDULE OVERVIEW                            ");
        _logger.LogInformation("═══════════════════════════════════════════════════════════════════════════");
        _logger.LogInformation("Schedule initialized at: {StartTime:yyyy-MM-dd HH:mm:ss} UTC", startTime);
        _logger.LogInformation("");
        _logger.LogInformation("{JobName,-40} {CronExpression,-20} {Duration,10} {NextRun,20}",
            "Job Name", "Cron Expression", "Duration", "First Run (UTC)");
        _logger.LogInformation("───────────────────────────────────────────────────────────────────────────");

        foreach (var job in schedule.OrderBy(j => j.FirstRun ?? DateTime.MaxValue))
        {
            var nextRunStr = job.FirstRun?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
            var durationStr = $"{job.ExpectedDuration}s";

            _logger.LogInformation("{JobName,-40} {CronExpression,-20} {ExpectedDuration,10} {NextRun,20}",
                job.Name, job.CronExpression, durationStr, nextRunStr);
        }

        _logger.LogInformation("═══════════════════════════════════════════════════════════════════════════");
        _logger.LogInformation("");

    }
}

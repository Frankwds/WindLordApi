using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WindLordApi.Worker.Services;

namespace WindLordApi.Worker.Startup;

/// <summary>
/// Startup jobs runner that executes all startup tasks.
/// </summary>
public static class StartupJobs
{
    /// <summary>
    /// Runs all startup jobs and logs their execution.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve services.</param>
    /// <param name="logger">The logger to use for logging startup job results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    public static async Task RunStartupJobsAsync(
        IServiceProvider serviceProvider,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running startup jobs...");

        // Sync all stations on startup
        try
        {
            using var scope = serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IMetFrostSyncService>();
            await syncService.SyncLatestStationDataAsync(cancellationToken);
            logger.LogInformation("Completed startup job: SyncLatestStationDataAsync");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running SyncLatestStationDataAsync on startup");
        }

        // Sync new weather stations on startup
        try
        {
            using var scope = serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IMetFrostSyncService>();
            await syncService.SyncWeatherStationsAsync(cancellationToken);
            logger.LogInformation("Completed startup job: SyncWeatherStationsAsync");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running SyncWeatherStationsAsync on startup");
        }

        // Sync weather station active status on startup
        try
        {
            using var scope = serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IMetFrostSyncService>();
            await syncService.SyncWeatherStationsActiveStatusAsync(cancellationToken);
            logger.LogInformation("Completed startup job: SyncWeatherStationsActiveStatusAsync");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running SyncWeatherStationsActiveStatusAsync on startup");
        }

        logger.LogInformation("Startup jobs completed");
    }
}


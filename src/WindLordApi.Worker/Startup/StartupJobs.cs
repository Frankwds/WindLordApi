using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WindLordApi.Worker.Services;
using WindLordApi.Integrations.MetYr;
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

        // Retain only recent station observations on startup
        try
        {
            using var stationDataRetentionScope = serviceProvider.CreateScope();
            var stationDataRetentionService = stationDataRetentionScope.ServiceProvider.GetRequiredService<IStationDataRetentionService>();
            await stationDataRetentionService.CleanupOldObservationsAsync(cancellationToken);
            logger.LogInformation("StationDataRetention: Completed startup job: CleanupOldObservationsAsync");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "StationDataRetention: Error running CleanupOldObservationsAsync on startup");
        }

        // Refresh Open-Meteo forecasts
        try
        {
            using var openMeteoForecastSupplementScope = serviceProvider.CreateScope();
            var openMeteoForecastSupplementService = openMeteoForecastSupplementScope.ServiceProvider.GetRequiredService<IOpenMeteoForecastSupplementService>();

            await openMeteoForecastSupplementService.SupplementForecastsAsync(cancellationToken);
            logger.LogInformation("OpenMeteoForecastSupplement: Completed startup job: SupplementForecastsAsync");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OpenMeteoForecastSupplement: Error running SupplementForecastsAsync on startup");
        }

        // Refresh authoritative MetYr forecasts
        try
        {
            using var metYrForecastRefreshScope = serviceProvider.CreateScope();
            var metYrForecastRefreshService = metYrForecastRefreshScope.ServiceProvider.GetRequiredService<IMetYrForecastRefreshService>();

            await metYrForecastRefreshService.UpdateForecastsAsync(cancellationToken);
            logger.LogInformation("MetYrForecastRefresh: Completed startup job: UpdateForecastsAsync");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MetYrForecastRefresh: Error running UpdateForecastsAsync on startup");
        }

        // Refresh PortWind stations and data
        try
        {
            using var scope = serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IPortWindStationRefreshService>();
            await syncService.SyncWeatherStationsAsync(cancellationToken);
            logger.LogInformation("PortWind: Completed startup job: SyncWeatherStationsAsync");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PortWind: Error running SyncWeatherStationsAsync on startup");
        }

        // Refresh latest PortWind station data on startup
        try
        {
            using var scope = serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IPortWindLatestDataSyncService>();
            await syncService.SyncLatestStationDataAsync(cancellationToken);
            logger.LogInformation("PortWind: Completed startup job: SyncLatestStationDataAsync");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PortWind: Error running SyncLatestStationDataAsync on startup");
        }

        // Sync WindsMobi data on startup
        try
        {
            using var windsMobiScope = serviceProvider.CreateScope();
            var windsMobiSyncService = windsMobiScope.ServiceProvider.GetRequiredService<IWindsMobiSyncService>();
            await windsMobiSyncService.SyncWindsMobiDataAsync(cancellationToken);
            logger.LogInformation("WindsMobi: Completed startup job: SyncWindsMobiDataAsync");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WindsMobi: Error running SyncWindsMobiDataAsync on startup");
        }

        // Locate countries for stations with missing country data
        try
        {
            using var countryLocatorScope = serviceProvider.CreateScope();
            var countryLocatorService = countryLocatorScope.ServiceProvider.GetRequiredService<ICountryLocatorService>();
            await countryLocatorService.LocateCountriesAsync(cancellationToken);
            logger.LogInformation("CountryLocator: Completed startup job: LocateCountriesAsync");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CountryLocator: Error running LocateCountriesAsync on startup");
        }

        // Sync Holfuy data on startup 
        try
        {
            using var holfuyScope = serviceProvider.CreateScope();
            var holfuySyncService = holfuyScope.ServiceProvider.GetRequiredService<IHolfuySyncService>();
            await holfuySyncService.SyncHolfuyDataAsync(cancellationToken);
            logger.LogInformation("Holfuy: Completed startup job: SyncHolfuyDataAsync");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Holfuy: Error running SyncHolfuyDataAsync on startup");
        }

        // Sync all weather stations on startup
        try
        {
            using var scope = serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IMetFrostSyncService>();
            await syncService.SyncLatestStationDataAsync(cancellationToken);
            logger.LogInformation("MetFrost: Completed startup job: SyncLatestStationDataAsync");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MetFrost: Error running SyncLatestStationDataAsync on startup");
        }

        // Sync new weather stations on startup
        try
        {
            using var scope = serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IMetFrostSyncService>();
            await syncService.SyncWeatherStationsAsync(cancellationToken);
            logger.LogInformation("MetFrost: Completed startup job: SyncWeatherStationsAsync");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MetFrost: Error running SyncWeatherStationsAsync on startup");
        }

        // Sync weather station active status on startup
        try
        {
            using var scope = serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IMetFrostSyncService>();
            await syncService.SyncWeatherStationsActiveStatusAsync(cancellationToken);
            logger.LogInformation("MetFrost: Completed startup job: SyncWeatherStationsActiveStatusAsync");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MetFrost: Error running SyncWeatherStationsActiveStatusAsync on startup");
        }

        logger.LogInformation("Startup jobs completed");
    }
}


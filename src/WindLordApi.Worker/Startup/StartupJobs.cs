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


        // Update forecasts on startup
        try
        {
            using var forecastUpdateScope = serviceProvider.CreateScope();
            var forecastUpdateService = forecastUpdateScope.ServiceProvider.GetRequiredService<IForecastUpdateService>();

            await forecastUpdateService.UpdateForecastsAsync(cancellationToken);
            logger.LogInformation("ForecastUpdate: Completed startup job: UpdateForecastsAsync");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ForecastUpdate: Error running UpdateForecastsAsync on startup");
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


using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WindLordApi.Worker.Services;
using WindLordApi.Integrations.MetYr;
using WindLordApi.Integrations.OpenMeteo;
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

        // Test Forecast Combination Service
        try
        {
            using var combinationScope = serviceProvider.CreateScope();
            var openMeteoClient = combinationScope.ServiceProvider.GetRequiredService<IOpenMeteoClient>();
            var openMeteoMapping = combinationScope.ServiceProvider.GetRequiredService<IOpenMeteoMapping>();
            var metYrClient = combinationScope.ServiceProvider.GetRequiredService<IMetYrClient>();
            var metYrMapping = combinationScope.ServiceProvider.GetRequiredService<IMetYrMapping>();
            var forecastCombinationService = combinationScope.ServiceProvider.GetRequiredService<IForecastCombinationService>();

            // Fetch and map OpenMeteo data
            var openMeteoRawData = await openMeteoClient.FetchMeteoDataAsync(63.458, 11.682, cancellationToken);
            var openMeteoWeatherData = openMeteoMapping.MapOpenMeteoData(openMeteoRawData);
            logger.LogInformation("ForecastCombination: Fetched {Count} OpenMeteo data points", openMeteoWeatherData.Count);

            // Fetch and map MetYr data
            var metYrRawData = await metYrClient.FetchYrDataAsync(63.458, 11.682, cancellationToken);
            var metYrWeatherData = metYrMapping.MapYrData(metYrRawData);
            logger.LogInformation("ForecastCombination: Fetched {Count} MetYr hourly data points", metYrWeatherData.MetYrDto.Count);

            // Combine the data sources
            // Using a test location ID for the hardcoded coordinates (63.458, 11.682)
            var testLocationId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var combinedData = forecastCombinationService.CombineDataSources(
                openMeteoWeatherData,
                metYrWeatherData.MetYrDto,
                testLocationId);

            logger.LogInformation("ForecastCombination: Successfully combined {Count} forecast data points", combinedData.Count);

            // Log some sample data from the first combined point
            if (combinedData.Count > 0)
            {
                var firstPoint = combinedData[0];
                logger.LogInformation(
                    "ForecastCombination: Sample data - Time: {Time}, Temperature: {Temp}°C, WindSpeed: {WindSpeed}m/s, IsYrData: {IsYrData}, WeatherCode: {WeatherCode}",
                    firstPoint.Time,
                    firstPoint.Temperature,
                    firstPoint.WindSpeed,
                    firstPoint.IsYrData,
                    firstPoint.WeatherCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ForecastCombination: Error running test");
        }
        // Sync Holfuy data on startup (first)
        // try
        // {
        //     using var holfuyScope = serviceProvider.CreateScope();
        //     var holfuySyncService = holfuyScope.ServiceProvider.GetRequiredService<IHolfuySyncService>();
        //     await holfuySyncService.SyncHolfuyDataAsync(cancellationToken);
        //     logger.LogInformation("Holfuy: Completed startup job: SyncHolfuyDataAsync");
        // }
        // catch (Exception ex)
        // {
        //     logger.LogError(ex, "Holfuy: Error running SyncHolfuyDataAsync on startup");
        // }

        // // Sync all stations on startup
        // try
        // {
        //     using var scope = serviceProvider.CreateScope();
        //     var syncService = scope.ServiceProvider.GetRequiredService<IMetFrostSyncService>();
        //     await syncService.SyncLatestStationDataAsync(cancellationToken);
        //     logger.LogInformation("MetFrost: Completed startup job: SyncLatestStationDataAsync");
        // }
        // catch (Exception ex)
        // {
        //     logger.LogError(ex, "MetFrost: Error running SyncLatestStationDataAsync on startup");
        // }

        // // Sync new weather stations on startup
        // try
        // {
        //     using var scope = serviceProvider.CreateScope();
        //     var syncService = scope.ServiceProvider.GetRequiredService<IMetFrostSyncService>();
        //     await syncService.SyncWeatherStationsAsync(cancellationToken);
        //     logger.LogInformation("MetFrost: Completed startup job: SyncWeatherStationsAsync");
        // }
        // catch (Exception ex)
        // {
        //     logger.LogError(ex, "MetFrost: Error running SyncWeatherStationsAsync on startup");
        // }

        // // Sync weather station active status on startup
        // try
        // {
        //     using var scope = serviceProvider.CreateScope();
        //     var syncService = scope.ServiceProvider.GetRequiredService<IMetFrostSyncService>();
        //     await syncService.SyncWeatherStationsActiveStatusAsync(cancellationToken);
        //     logger.LogInformation("MetFrost: Completed startup job: SyncWeatherStationsActiveStatusAsync");
        // }
        // catch (Exception ex)
        // {
        //     logger.LogError(ex, "MetFrost: Error running SyncWeatherStationsActiveStatusAsync on startup");
        // }

        logger.LogInformation("Startup jobs completed");
    }
}


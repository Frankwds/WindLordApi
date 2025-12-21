using Microsoft.Extensions.Logging;
using WindLordApi.Data.Models;
using WindLordApi.Data.Services;
using WindLordApi.Integrations.MetYr;
using WindLordApi.Integrations.OpenMeteo;

namespace WindLordApi.Worker.Services;

/// <summary>
/// Service for updating forecast data for paragliding locations.
/// Implements the Next.js cron job logic for combining OpenMeteo and MetYr data.
/// </summary>
public class ForecastUpdateService : IForecastUpdateService
{
    private readonly IOpenMeteoClient _openMeteoClient;
    private readonly IMetYrClient _metYrClient;
    private readonly IOpenMeteoMapping _openMeteoMapping;
    private readonly IMetYrMapping _metYrMapping;
    private readonly IForecastCombinationService _forecastCombinationService;
    private readonly IParaglidingLocationService _paraglidingLocationService;
    private readonly IForecastCacheService _forecastCacheService;
    private readonly ILogger<ForecastUpdateService> _logger;

    private const int BatchSize = 50;

    public ForecastUpdateService(
        IOpenMeteoClient openMeteoClient,
        IMetYrClient metYrClient,
        IOpenMeteoMapping openMeteoMapping,
        IMetYrMapping metYrMapping,
        IForecastCombinationService forecastCombinationService,
        IParaglidingLocationService paraglidingLocationService,
        IForecastCacheService forecastCacheService,
        ILogger<ForecastUpdateService> logger)
    {
        _openMeteoClient = openMeteoClient;
        _metYrClient = metYrClient;
        _openMeteoMapping = openMeteoMapping;
        _metYrMapping = metYrMapping;
        _forecastCombinationService = forecastCombinationService;
        _paraglidingLocationService = paraglidingLocationService;
        _forecastCacheService = forecastCacheService;
        _logger = logger;
    }

    public async Task UpdateForecastsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting forecast update");

        try
        {
            await CleanupOldForecastDataAsync(cancellationToken);
            await ProcessLocationsWithOldestForecastDataAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background forecast update failed");
            throw;
        }

        _logger.LogInformation("Forecast update completed successfully");
    }

    private async Task CleanupOldForecastDataAsync(CancellationToken cancellationToken)
    {
        var twoHoursAgo = DateTime.UtcNow.AddHours(-2);

        _logger.LogInformation("Deleting forecast data older than: {CutoffTime}", twoHoursAgo);

        await _forecastCacheService.DeleteOldForecastsAsync(twoHoursAgo, cancellationToken);

        _logger.LogInformation("Forecast data cleanup completed successfully");
    }

    private async Task ProcessLocationsWithOldestForecastDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Get locations without any forecast data (up to BATCH_SIZE)
            var locationsWithoutForecast = await _paraglidingLocationService.GetLocationsWithoutForecastAsync(BatchSize, cancellationToken);
            var locationIdsNoData = locationsWithoutForecast
                .Select(l => l.LocationId)
                .ToList();

            var remainingSlots = BatchSize - locationIdsNoData.Count;
            var locationIds = new List<Guid>(locationIdsNoData);

            // Fill remaining slots with locations that have oldest forecast data
            if (remainingSlots > 0)
            {
                var locationsWithOldest = await _paraglidingLocationService.GetLocationsWithOldestForecastAsync(remainingSlots, cancellationToken);
                var locationIdsOldestData = locationsWithOldest
                    .Select(l => l.LocationId)
                    .ToList();

                locationIds.AddRange(locationIdsOldestData);
            }

            // Fetch full location details by IDs
            var locations = await _paraglidingLocationService.GetByIdsAsync(locationIds, cancellationToken);
            var locationsList = locations.ToList();

            _logger.LogInformation("Processing {Count} locations total", locationsList.Count);

            if (locationsList.Count > 0)
            {
                await ProcessBatchAsync(locationsList, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process oldest locations");
            throw;
        }
    }

    private async Task ProcessBatchAsync(List<ParaglidingLocation> locations, CancellationToken cancellationToken)
    {
        var latitudes = locations.Select(l => l.Latitude).ToArray();
        var longitudes = locations.Select(l => l.Longitude).ToArray();

        // Fetch batch data from OpenMeteo for all locations
        var rawMeteoDataArray = await _openMeteoClient.FetchMeteoDataAsync(latitudes, longitudes, cancellationToken);

        // Process each location individually
        for (int index = 0; index < locations.Count; index++)
        {
            var location = locations[index];
            try
            {
                _logger.LogInformation("Processing location {LocationId} ({Index}/{Total})", location.Id, index + 1, locations.Count);

                // Get OpenMeteo data for this specific location from the array
                var rawMeteoData = rawMeteoDataArray[index];
                var meteoData = _openMeteoMapping.MapOpenMeteoData(rawMeteoData);

                // Fetch YR data for takeoff location
                _logger.LogDebug("Fetching MetYr data for takeoff location {LocationId}", location.Id);
                var yrTakeoffData = await _metYrClient.FetchYrDataAsync(location.Latitude, location.Longitude, cancellationToken);
                var mappedYrTakeoffData = _metYrMapping.MapYrData(yrTakeoffData);

                // Combine data sources
                var combinedData = _forecastCombinationService.CombineDataSources(
                    meteoData,
                    mappedYrTakeoffData.MetYrDto,
                    location.Id);

                // If landing coordinates exist, fetch and merge landing data
                if (location.LandingLatitude.HasValue && location.LandingLongitude.HasValue)
                {
                    _logger.LogDebug("Fetching MetYr data for landing location {LocationId}", location.Id);
                    var yrLandingData = await _metYrClient.FetchYrDataAsync(
                        location.LandingLatitude.Value,
                        location.LandingLongitude.Value,
                        cancellationToken);
                    var mappedYrLandingData = _metYrMapping.MapYrData(yrLandingData);

                    combinedData = MergeLandingData(combinedData, mappedYrLandingData.MetYrDto);
                }

                // Upsert forecast data
                _logger.LogDebug("Upserting {Count} forecast records for location {LocationId}", combinedData.Count, location.Id);
                await _forecastCacheService.UpsertManyAsync(combinedData.ToArray(), cancellationToken);

                _logger.LogDebug("Successfully processed location {LocationId}", location.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process location {LocationId}", location.Id);
                // Continue processing other locations
            }
        }
    }

    /// <summary>
    /// Merges landing wind data into combined forecast data by matching time strings.
    /// </summary>
    private IReadOnlyList<ForecastCache> MergeLandingData(
        IReadOnlyList<ForecastCache> combinedData,
        IReadOnlyList<MetYrDto> landingData)
    {
        // Create a dictionary of landing data keyed by time (first 16 characters)
        var landingDataMap = new Dictionary<string, MetYrDto>();
        foreach (var landingDp in landingData)
        {
            var timeKey = landingDp.Time.Length >= 16 ? landingDp.Time.Substring(0, 16) : landingDp.Time;
            if (!landingDataMap.ContainsKey(timeKey))
            {
                landingDataMap[timeKey] = landingDp;
            }
        }

        var result = new List<ForecastCache>();
        foreach (var dataPoint in combinedData)
        {
            // Format the time to match Yr format (YYYY-MM-DDTHH:MM)
            var timeKey = dataPoint.Time.ToString("yyyy-MM-ddTHH:mm");

            if (landingDataMap.TryGetValue(timeKey, out var landingDataPoint))
            {
                // Update landing data fields in place
                dataPoint.LandingWind = landingDataPoint.WindSpeed;
                dataPoint.LandingGust = landingDataPoint.WindSpeedOfGust;
                dataPoint.LandingWindDirection = (int)Math.Round(landingDataPoint.WindFromDirection);
            }

            result.Add(dataPoint);
        }

        return result;
    }
}


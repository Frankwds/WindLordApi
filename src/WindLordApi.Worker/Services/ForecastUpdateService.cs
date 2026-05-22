using Microsoft.Extensions.Logging;
using WindLordApi.Data.Models;
using WindLordApi.Data.Services;
using WindLordApi.Integrations.MetYr;
using WindLordApi.Integrations.OpenMeteo;

namespace WindLordApi.Worker.Services;

/// <summary>
/// Service for updating forecast data for paragliding locations.
/// Fetches MetYr forecast data and supplements later timestamps with Open-Meteo forecast data.
/// </summary>
public class ForecastUpdateService : IForecastUpdateService
{
    private readonly IMetYrClient _metYrClient;
    private readonly IMetYrMapping _metYrMapping;
    private readonly IOpenMeteoClient _openMeteoClient;
    private readonly IOpenMeteoMapping _openMeteoMapping;
    private readonly IParaglidingLocationService _paraglidingLocationService;
    private readonly IForecastCacheService _forecastCacheService;
    private readonly ILogger<ForecastUpdateService> _logger;

    private const int BatchSize = 50;

    private sealed record ProcessedLocationForecast(
        int LocationIndex,
        ParaglidingLocation Location,
        IReadOnlyList<ForecastCache> ForecastData);

    public ForecastUpdateService(
        IMetYrClient metYrClient,
        IMetYrMapping metYrMapping,
        IOpenMeteoClient openMeteoClient,
        IOpenMeteoMapping openMeteoMapping,
        IParaglidingLocationService paraglidingLocationService,
        IForecastCacheService forecastCacheService,
        ILogger<ForecastUpdateService> logger)
    {
        _metYrClient = metYrClient;
        _metYrMapping = metYrMapping;
        _openMeteoClient = openMeteoClient;
        _openMeteoMapping = openMeteoMapping;
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

        var deletedCount = await _forecastCacheService.DeleteOldForecastsAsync(twoHoursAgo, cancellationToken);

        _logger.LogInformation("Forecast data cleanup completed successfully. Deleted {Count} records of forecast cache", deletedCount);
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
        var currentTime = DateTime.UtcNow;
        var processedLocations = new List<ProcessedLocationForecast>(locations.Count);
        var openMeteoForecastTask = FetchOpenMeteoForecastsAsync(locations, currentTime, cancellationToken);

        for (int index = 0; index < locations.Count; index++)
        {
            var location = locations[index];
            try
            {
                _logger.LogDebug("Processing location {LocationId} ({Index}/{Total})", location.Id, index + 1, locations.Count);

                // Fetch MetYr data for takeoff location
                _logger.LogDebug("Fetching MetYr data for takeoff location {LocationId}", location.Id);
                var yrTakeoffData = await _metYrClient.FetchYrDataAsync(location.Latitude, location.Longitude, cancellationToken);
                var mappedYrTakeoffData = _metYrMapping.MapYrData(yrTakeoffData);

                // Convert MetYr data to ForecastCache
                var forecastData = ConvertToForecastCache(mappedYrTakeoffData.MetYrDto, location.Id, currentTime);

                // If landing coordinates exist, fetch and merge landing data
                if (location.LandingLatitude.HasValue && location.LandingLongitude.HasValue)
                {
                    _logger.LogDebug("Fetching MetYr data for landing location {LocationId}", location.Id);
                    var yrLandingData = await _metYrClient.FetchYrDataAsync(
                        location.LandingLatitude.Value,
                        location.LandingLongitude.Value,
                        cancellationToken);
                    var mappedYrLandingData = _metYrMapping.MapYrData(yrLandingData);

                    forecastData = MergeLandingData(forecastData, mappedYrLandingData.MetYrDto);
                }

                processedLocations.Add(new ProcessedLocationForecast(index, location, forecastData));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process location {LocationId}", location.Id);
                // Continue processing other locations
            }
        }

        var openMeteoForecasts = await TryGetOpenMeteoForecastsAsync(locations, openMeteoForecastTask);

        foreach (var processedLocation in processedLocations)
        {
            var forecastData = MergeOpenMeteoData(
                processedLocation.ForecastData,
                processedLocation.Location,
                processedLocation.LocationIndex,
                openMeteoForecasts,
                currentTime);

            await _forecastCacheService.UpsertManyAsync(forecastData.ToArray(), cancellationToken);
        }
    }

    private async Task<IReadOnlyList<OpenMeteoLocationForecast>> FetchOpenMeteoForecastsAsync(
        IReadOnlyList<ParaglidingLocation> locations,
        DateTime currentTime,
        CancellationToken cancellationToken)
    {
        var requestLocations = locations
            .Select(location => new OpenMeteoRequestLocation(location.Latitude, location.Longitude))
            .ToArray();

        var openMeteoResponses = await _openMeteoClient.FetchForecastAsync(
            requestLocations,
            currentTime.AddHours(48),
            currentTime.AddHours(96),
            cancellationToken);

        return _openMeteoMapping.MapForecasts(openMeteoResponses);
    }

    private async Task<IReadOnlyList<OpenMeteoLocationForecast>?> TryGetOpenMeteoForecastsAsync(
        IReadOnlyList<ParaglidingLocation> locations,
        Task<IReadOnlyList<OpenMeteoLocationForecast>> openMeteoForecastTask)
    {
        try
        {
            var openMeteoForecasts = await openMeteoForecastTask;
            ValidateOpenMeteoForecasts(locations, openMeteoForecasts);
            return openMeteoForecasts;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Open-Meteo forecast supplement batch failed for {LocationCount} locations. Persisting Yr-only rows.",
                locations.Count);
            return null;
        }
    }

    private static void ValidateOpenMeteoForecasts(
        IReadOnlyList<ParaglidingLocation> locations,
        IReadOnlyList<OpenMeteoLocationForecast> openMeteoForecasts)
    {
        if (openMeteoForecasts.Count != locations.Count)
        {
            throw new InvalidOperationException(
                $"Open-Meteo returned {openMeteoForecasts.Count} location blocks for {locations.Count} requested locations.");
        }

        for (int index = 0; index < locations.Count; index++)
        {
            var requestedLocation = locations[index];
            var openMeteoForecast = openMeteoForecasts[index];

            if (!OpenMeteoCoordinates.MatchesRequestPrecision(openMeteoForecast.Latitude, requestedLocation.Latitude)
                || !OpenMeteoCoordinates.MatchesRequestPrecision(openMeteoForecast.Longitude, requestedLocation.Longitude))
            {
                throw new InvalidOperationException(
                    $"Open-Meteo response block at index {index} did not match the requested coordinates for location '{requestedLocation.Id}'.");
            }
        }
    }

    private IReadOnlyList<ForecastCache> MergeOpenMeteoData(
        IReadOnlyList<ForecastCache> yrForecastData,
        ParaglidingLocation location,
        int locationIndex,
        IReadOnlyList<OpenMeteoLocationForecast>? openMeteoForecasts,
        DateTime currentTime)
    {
        if (openMeteoForecasts is null || yrForecastData.Count == 0)
        {
            return yrForecastData;
        }

        var latestYrTimestamp = yrForecastData.Max(forecast => forecast.Time);
        var supplementalRows = openMeteoForecasts[locationIndex].Forecasts
            .Where(forecast => forecast.Time > latestYrTimestamp)
            .Select(forecast => ConvertToForecastCache(forecast, location.Id, currentTime))
            .ToArray();

        if (supplementalRows.Length == 0)
        {
            return yrForecastData;
        }

        return yrForecastData
            .Concat(supplementalRows)
            .OrderBy(forecast => forecast.Time)
            .ToArray();
    }

    /// <summary>
    /// Converts MetYr data to ForecastCache entities.
    /// </summary>
    private IReadOnlyList<ForecastCache> ConvertToForecastCache(
        IReadOnlyList<MetYrDto> yrData,
        Guid locationId,
        DateTime currentTime)
    {
        var result = new List<ForecastCache>();

        foreach (var yrDp in yrData)
        {
            // Determine isDay: if symbol_code includes 'night', set to 0, otherwise set to 1
            short? isDay = yrDp.SymbolCode.Contains("night", StringComparison.OrdinalIgnoreCase) ? (short)0 : (short)1;

            // Parse the time string to DateTime
            DateTime timeValue = DateTime.Parse(yrDp.Time, null, System.Globalization.DateTimeStyles.AdjustToUniversal);

            var forecastCache = new ForecastCache
            {
                // Basic identification
                Time = timeValue,
                LocationId = locationId,
                IsYrData = true,
                UpdatedAt = currentTime,
                CreatedAt = currentTime,

                // Surface conditions from MetYr
                Temperature = yrDp.AirTemperature,
                WindSpeed = yrDp.WindSpeed,
                WindDirection = (int)Math.Truncate(yrDp.WindFromDirection),
                WindGusts = yrDp.WindSpeedOfGust,
                Precipitation = yrDp.PrecipitationAmount,
                PrecipitationMax = yrDp.PrecipitationAmountMax,
                PrecipitationMin = yrDp.PrecipitationAmountMin,
                PrecipitationProbability = yrDp.ProbabilityOfPrecipitation,
                PressureMsl = yrDp.AirPressureAtSeaLevel,
                WeatherCode = yrDp.SymbolCode,
                IsDay = isDay,

                // Landing conditions (not yet populated)
                LandingWind = null,
                LandingGust = null,
                LandingWindDirection = null,

                // Atmospheric conditions - set to null (not provided by MetYr)
                WindSpeed1000hpa = null,
                WindDirection1000hpa = null,
                WindSpeed925hpa = null,
                WindDirection925hpa = null,
                WindSpeed850hpa = null,
                WindDirection850hpa = null,
                WindSpeed700hpa = null,
                WindDirection700hpa = null,
                Temperature1000hpa = null,
                Temperature925hpa = null,
                Temperature850hpa = null,
                Temperature700hpa = null,
                CloudCover = null,
                CloudCoverLow = null,
                CloudCoverMid = null,
                CloudCoverHigh = null,
                Cape = null,
                ConvectiveInhibition = null,
                LiftedIndex = null,
                BoundaryLayerHeight = null,
                FreezingLevelHeight = null,
                GeopotentialHeight1000hpa = null,
                GeopotentialHeight925hpa = null,
                GeopotentialHeight850hpa = null,
                GeopotentialHeight700hpa = null
            };

            result.Add(forecastCache);
        }

        return result;
    }

    private static ForecastCache ConvertToForecastCache(
        OpenMeteoForecastPoint forecastPoint,
        Guid locationId,
        DateTime currentTime)
    {
        return new ForecastCache
        {
            Time = forecastPoint.Time,
            LocationId = locationId,
            IsYrData = false,
            UpdatedAt = currentTime,
            CreatedAt = currentTime,
            Temperature = forecastPoint.Temperature,
            WindSpeed = forecastPoint.WindSpeed,
            WindDirection = forecastPoint.WindDirection,
            WindGusts = forecastPoint.WindGusts,
            Precipitation = forecastPoint.Precipitation,
            PrecipitationMax = null,
            PrecipitationMin = null,
            PrecipitationProbability = forecastPoint.PrecipitationProbability,
            PressureMsl = forecastPoint.PressureMsl,
            WeatherCode = forecastPoint.WeatherCode,
            IsDay = forecastPoint.IsDay,
            LandingWind = null,
            LandingGust = null,
            LandingWindDirection = null,
            WindSpeed1000hpa = null,
            WindDirection1000hpa = null,
            WindSpeed925hpa = null,
            WindDirection925hpa = null,
            WindSpeed850hpa = null,
            WindDirection850hpa = null,
            WindSpeed700hpa = null,
            WindDirection700hpa = null,
            Temperature1000hpa = null,
            Temperature925hpa = null,
            Temperature850hpa = null,
            Temperature700hpa = null,
            CloudCover = null,
            CloudCoverLow = null,
            CloudCoverMid = null,
            CloudCoverHigh = null,
            Cape = null,
            ConvectiveInhibition = null,
            LiftedIndex = null,
            BoundaryLayerHeight = null,
            FreezingLevelHeight = null,
            GeopotentialHeight1000hpa = null,
            GeopotentialHeight925hpa = null,
            GeopotentialHeight850hpa = null,
            GeopotentialHeight700hpa = null
        };
    }

    /// <summary>
    /// Merges landing wind data into forecast data by matching time strings.
    /// </summary>
    private IReadOnlyList<ForecastCache> MergeLandingData(
        IReadOnlyList<ForecastCache> forecastData,
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
        foreach (var dataPoint in forecastData)
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


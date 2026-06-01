using Microsoft.Extensions.Logging;
using WindLordApi.Data.Models;
using WindLordApi.Data.Services;
using WindLordApi.Integrations.OpenMeteo;

namespace WindLordApi.Worker.Services;

/// <summary>
/// Service for supplementing takeoff forecast coverage with Open-Meteo data.
/// </summary>
public class OpenMeteoForecastSupplementService : IOpenMeteoForecastSupplementService
{
    private const int BatchSize = 50;

    private readonly IOpenMeteoClient _openMeteoClient;
    private readonly IOpenMeteoMapping _openMeteoMapping;
    private readonly IParaglidingLocationService _paraglidingLocationService;
    private readonly IForecastCacheService _forecastCacheService;
    private readonly ILogger<OpenMeteoForecastSupplementService> _logger;

    public OpenMeteoForecastSupplementService(
        IOpenMeteoClient openMeteoClient,
        IOpenMeteoMapping openMeteoMapping,
        IParaglidingLocationService paraglidingLocationService,
        IForecastCacheService forecastCacheService,
        ILogger<OpenMeteoForecastSupplementService> logger)
    {
        _openMeteoClient = openMeteoClient;
        _openMeteoMapping = openMeteoMapping;
        _paraglidingLocationService = paraglidingLocationService;
        _forecastCacheService = forecastCacheService;
        _logger = logger;
    }

    public async Task SupplementForecastsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Open-Meteo forecast supplement run");

        try
        {
            var locations = await SelectLocationsAsync(cancellationToken);
            if (locations.Count == 0)
            {
                _logger.LogInformation("No locations selected for Open-Meteo forecast supplementation");
                return;
            }

            await ProcessBatchAsync(locations, cancellationToken);
            _logger.LogInformation("Open-Meteo forecast supplement run completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Open-Meteo forecast supplement batch failed");
        }
    }

    private async Task<List<ParaglidingLocation>> SelectLocationsAsync(CancellationToken cancellationToken)
    {
        var locationIds = (await _paraglidingLocationService.GetOpenMeteoRefreshCandidatesAsync(BatchSize, cancellationToken)).ToList();
        var locations = await _paraglidingLocationService.GetByIdsAsync(locationIds, cancellationToken);
        return locations.ToList();
    }

    private async Task ProcessBatchAsync(IReadOnlyList<ParaglidingLocation> locations, CancellationToken cancellationToken)
    {
        var currentTime = DateTime.UtcNow;
        var requestLocations = locations
            .Select(location => new OpenMeteoRequestLocation(location.Latitude, location.Longitude))
            .ToArray();

        var openMeteoResponses = await _openMeteoClient.FetchForecastAsync(
            requestLocations,
            currentTime.AddHours(48),
            currentTime.AddHours(96),
            cancellationToken);

        var openMeteoForecasts = _openMeteoMapping.MapForecasts(openMeteoResponses);
        ValidateOpenMeteoForecasts(locations, openMeteoForecasts);

        for (int index = 0; index < locations.Count; index++)
        {
            var location = locations[index];
            var forecastData = openMeteoForecasts[index].Forecasts
                .Select(forecast => ConvertToForecastCache(forecast, location.Id, currentTime))
                .ToArray();

            if (forecastData.Length == 0)
            {
                continue;
            }

            await _forecastCacheService.UpsertManyAsync(forecastData, cancellationToken);
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
            WindGusts = null,
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
        };
    }
}
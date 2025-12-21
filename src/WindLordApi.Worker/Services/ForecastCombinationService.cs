using WindLordApi.Data.Models;
using WindLordApi.Integrations.MetYr;
using WindLordApi.Integrations.OpenMeteo;

namespace WindLordApi.Worker.Services;

/// <summary>
/// Service for combining weather forecast data from OpenMeteo and MetYr APIs.
/// Implements the combineDataSources logic from Next.js.
/// </summary>
public class ForecastCombinationService : IForecastCombinationService
{
    /// <summary>
    /// Combines hourly weather data from OpenMeteo and MetYr APIs into a unified ForecastCache structure.
    /// </summary>
    /// <param name="meteoData">Hourly weather data points from OpenMeteo API.</param>
    /// <param name="yrData">Hourly weather data points from MetYr API.</param>
    /// <param name="locationId">Location ID.</param>
    /// <returns>Combined hourly forecast data points.</returns>
    public IReadOnlyList<ForecastCache> CombineDataSources(
        IReadOnlyList<OpenMeteoDto> meteoData,
        IReadOnlyList<MetYrDto> yrData,
        string locationId)
    {
        if (string.IsNullOrEmpty(locationId))
        {
            throw new ArgumentNullException(nameof(locationId));
        }

        // Create a dictionary from Yr data keyed by time (first 16 characters, removing timezone)
        var yrDataMap = new Dictionary<string, MetYrDto>();
        foreach (var yrDp in yrData)
        {
            // Remove the last 4 characters indicating timezone (e.g., ":00Z" or "+00:00")
            var timeKey = yrDp.Time.Length >= 16 ? yrDp.Time.Substring(0, 16) : yrDp.Time;
            if (!yrDataMap.ContainsKey(timeKey))
            {
                yrDataMap[timeKey] = yrDp;
            }
        }

        var result = new List<ForecastCache>();
        var currentTime = DateTime.UtcNow;

        foreach (var meteoDp in meteoData)
        {
            // Find matching Yr data point
            var yrDp = yrDataMap.TryGetValue(meteoDp.Time, out var matchedYrDp) ? matchedYrDp : null;

            // Combine the data
            var combined = CombineWeatherData(meteoDp, yrDp, currentTime, locationId);
            result.Add(combined);
        }

        return result;
    }

    /// <summary>
    /// Combines a single OpenMeteo data point with an optional MetYr data point.
    /// Implements the combineWeatherData logic from Next.js.
    /// </summary>
    private static ForecastCache CombineWeatherData(
        OpenMeteoDto meteoDataPoint,
        MetYrDto? yrDataPoint,
        DateTime currentTime,
        string locationId)
    {
        // Determine isDay: if Yr symbol_code includes 'night', set to 0, otherwise use OpenMeteo's IsDay
        int isDay = 0;
        if (yrDataPoint?.SymbolCode.Contains("night", StringComparison.OrdinalIgnoreCase) == true)
        {
            isDay = 0;
        }
        else
        {
            isDay = meteoDataPoint.IsDay;
        }

        return new ForecastCache
        {
            // Basic identification
            Time = meteoDataPoint.Time + ":00Z",
            LocationId = locationId,
            IsYrData = yrDataPoint != null,
            UpdatedAt = currentTime.ToUniversalTime().ToString("O"), // ISO 8601 format

            // Surface conditions
            Temperature = yrDataPoint?.AirTemperature ?? meteoDataPoint.Temperature2m,
            WindSpeed = yrDataPoint?.WindSpeed ?? meteoDataPoint.WindSpeed10m,
            WindDirection = (int)Math.Truncate(yrDataPoint?.WindFromDirection ?? meteoDataPoint.WindDirection10m),
            WindGusts = yrDataPoint?.WindSpeedOfGust,
            Precipitation = yrDataPoint?.PrecipitationAmount ?? meteoDataPoint.Precipitation,
            PrecipitationMax = yrDataPoint?.PrecipitationAmountMax ?? 0,
            PrecipitationMin = yrDataPoint?.PrecipitationAmountMin ?? 0,
            PrecipitationProbability = yrDataPoint?.ProbabilityOfPrecipitation ?? meteoDataPoint.PrecipitationProbability,
            PressureMsl = yrDataPoint?.AirPressureAtSeaLevel ?? meteoDataPoint.PressureMsl,
            WeatherCode = yrDataPoint?.SymbolCode ?? meteoDataPoint.WeatherCode,
            IsDay = isDay,

            // Landing conditions (not provided by either source, left as null)
            LandingWind = null,
            LandingGust = null,
            LandingWindDirection = null,

            // Atmospheric conditions - Wind at different pressure levels (from OpenMeteo)
            WindSpeed1000hpa = meteoDataPoint.WindSpeed1000hPa,
            WindDirection1000hpa = (int)Math.Truncate(meteoDataPoint.WindDirection1000hPa),
            WindSpeed925hpa = meteoDataPoint.WindSpeed925hPa,
            WindDirection925hpa = (int)Math.Truncate(meteoDataPoint.WindDirection925hPa),
            WindSpeed850hpa = meteoDataPoint.WindSpeed850hPa,
            WindDirection850hpa = (int)Math.Truncate(meteoDataPoint.WindDirection850hPa),
            WindSpeed700hpa = meteoDataPoint.WindSpeed700hPa,
            WindDirection700hpa = (int)Math.Truncate(meteoDataPoint.WindDirection700hPa),

            // Atmospheric conditions - Temperature at different pressure levels (from OpenMeteo)
            Temperature1000hpa = meteoDataPoint.Temperature1000hPa,
            Temperature925hpa = meteoDataPoint.Temperature925hPa,
            Temperature850hpa = meteoDataPoint.Temperature850hPa,
            Temperature700hpa = meteoDataPoint.Temperature700hPa,

            // Atmospheric conditions - Cloud cover (from OpenMeteo)
            CloudCover = meteoDataPoint.CloudCover,
            CloudCoverLow = meteoDataPoint.CloudCoverLow,
            CloudCoverMid = meteoDataPoint.CloudCoverMid,
            CloudCoverHigh = meteoDataPoint.CloudCoverHigh,

            // Atmospheric conditions - Stability and convection (from OpenMeteo)
            Cape = meteoDataPoint.Cape,
            ConvectiveInhibition = meteoDataPoint.ConvectiveInhibition,
            LiftedIndex = meteoDataPoint.LiftedIndex,
            BoundaryLayerHeight = meteoDataPoint.BoundaryLayerHeight,
            FreezingLevelHeight = meteoDataPoint.FreezingLevelHeight,

            // Atmospheric conditions - Geopotential heights (from OpenMeteo)
            GeopotentialHeight1000hpa = meteoDataPoint.GeopotentialHeight1000hPa,
            GeopotentialHeight925hpa = meteoDataPoint.GeopotentialHeight925hPa,
            GeopotentialHeight850hpa = meteoDataPoint.GeopotentialHeight850hPa,
            GeopotentialHeight700hpa = meteoDataPoint.GeopotentialHeight700hPa
        };
    }
}


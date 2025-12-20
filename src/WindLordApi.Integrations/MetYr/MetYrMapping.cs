using System.Text.Json;

namespace WindLordApi.Integrations.MetYr;

/// <summary>
/// Maps MET.no Locationforecast API response data to DTOs.
/// Implements the mapYrData logic from Next.js.
/// </summary>
public class MetYrMappingService : IMetYrMapping
{
    /// <summary>
    /// Maps raw API response to WeatherDataYr DTO.
    /// Implements the mapYrData logic from Next.js.
    /// </summary>
    /// <param name="rawData">Raw API response from MET.no Locationforecast API.</param>
    /// <returns>WeatherDataYr DTO with hourly and six-hourly forecast data.</returns>
    public WeatherDataYr MapYrData(MetYrResponse rawData)
    {
        if (rawData.Properties?.Timeseries == null || rawData.Properties.Timeseries.Count == 0)
        {
            return new WeatherDataYr
            {
                MetYrDto = Array.Empty<MetYrDto>(),
                UpdatedAt = rawData.Properties?.Meta?.UpdatedAt ?? string.Empty,
                Elevation = rawData.Geometry?.Coordinates?.Length >= 3 ? rawData.Geometry.Coordinates[2] : 0,
                Location = new LocationInfo
                {
                    Latitude = rawData.Geometry?.Coordinates?.Length >= 2 ? rawData.Geometry.Coordinates[1] : 0,
                    Longitude = rawData.Geometry?.Coordinates?.Length >= 1 ? rawData.Geometry.Coordinates[0] : 0
                }
            };
        }

        var timeseries = rawData.Properties.Timeseries;
        var firstMissingIndex = FindFirstMissingNext1HoursIndex(timeseries);

        // Slice hourly data: timeseries[0..(firstMissingIndex - 6)]
        var hourlyEndIndex = Math.Max(0, firstMissingIndex - 6);
        var slicedHourlyData = timeseries.Take(hourlyEndIndex).ToList();

        // Slice 6-hourly data: timeseries[firstMissingIndex..80]
        var sixHourlyStartIndex = firstMissingIndex;
        var sixHourlyEndIndex = Math.Min(timeseries.Count, 80);
        var slicedSixHourData = timeseries
            .Skip(sixHourlyStartIndex)
            .Take(sixHourlyEndIndex - sixHourlyStartIndex)
            .ToList();

        // Extract metadata
        var updatedAt = rawData.Properties.Meta.UpdatedAt;
        var coordinates = rawData.Geometry.Coordinates;
        var longitude = coordinates.Length >= 1 ? coordinates[0] : 0;
        var latitude = coordinates.Length >= 2 ? coordinates[1] : 0;
        var elevation = coordinates.Length >= 3 ? coordinates[2] : 0;

        // Map hourly data to WeatherDataPointYr1h[]
        var weatherDataPointYr1h = slicedHourlyData
            .Where(item => item.Data?.Next1Hours != null)
            .Select(item =>
            {
                var next1Hours = item.Data.Next1Hours!;

                // Parse next_6_hours (summary only for hourly entries)
                var next6HoursJson = item.Data.Next6Hours;
                var next6HoursSummary = JsonSerializer.Deserialize<MetYrNext6HoursForHourly>(next6HoursJson.GetRawText());

                // Parse instant details as 1-hour type
                var instantDetailsJson = item.Data.Instant.Details;
                var instant1Hour = JsonSerializer.Deserialize<MetYrInstantDetails1Hour>(instantDetailsJson.GetRawText());

                if (instant1Hour == null || next6HoursSummary == null)
                {
                    throw new InvalidOperationException("Failed to parse hourly forecast data");
                }

                return new MetYrDto
                {
                    Time = item.Time,
                    AirPressureAtSeaLevel = instant1Hour.AirPressureAtSeaLevel,
                    AirTemperature = instant1Hour.AirTemperature,
                    AirTemperaturePercentile10 = instant1Hour.AirTemperaturePercentile10,
                    AirTemperaturePercentile90 = instant1Hour.AirTemperaturePercentile90,
                    CloudAreaFraction = instant1Hour.CloudAreaFraction,
                    CloudAreaFractionHigh = instant1Hour.CloudAreaFractionHigh,
                    CloudAreaFractionLow = instant1Hour.CloudAreaFractionLow,
                    CloudAreaFractionMedium = instant1Hour.CloudAreaFractionMedium,
                    DewPointTemperature = instant1Hour.DewPointTemperature,
                    RelativeHumidity = instant1Hour.RelativeHumidity,
                    WindFromDirection = instant1Hour.WindFromDirection,
                    WindSpeed = instant1Hour.WindSpeed,
                    PrecipitationAmount = next1Hours.Details.PrecipitationAmount,
                    PrecipitationAmountMax = next1Hours.Details.PrecipitationAmountMax,
                    PrecipitationAmountMin = next1Hours.Details.PrecipitationAmountMin,
                    ProbabilityOfPrecipitation = next1Hours.Details.ProbabilityOfPrecipitation,
                    SymbolCode = next1Hours.Summary.SymbolCode,
                    FogAreaFraction = instant1Hour.FogAreaFraction,
                    UltravioletIndexClearSky = instant1Hour.UltravioletIndexClearSky,
                    WindSpeedOfGust = instant1Hour.WindSpeedOfGust,
                    ProbabilityOfThunder = next1Hours.Details.ProbabilityOfThunder,
                    Next6HoursSymbolCode = next6HoursSummary.Summary.SymbolCode
                };
            })
            .ToList();

        return new WeatherDataYr
        {
            MetYrDto = weatherDataPointYr1h,
            UpdatedAt = updatedAt,
            Elevation = elevation,
            Location = new LocationInfo
            {
                Latitude = latitude,
                Longitude = longitude
            }
        };
    }

    /// <summary>
    /// Finds the first index where next_1_hours is missing.
    /// Mirrors the Next.js function: findFirstMissingNext1HoursIndex.
    /// </summary>
    private static int FindFirstMissingNext1HoursIndex(IReadOnlyList<MetYrTimeSeries> timeseries)
    {
        for (int i = 0; i < timeseries.Count; i++)
        {
            if (timeseries[i].Data?.Next1Hours == null)
            {
                return i;
            }
        }
        return timeseries.Count;
    }
}


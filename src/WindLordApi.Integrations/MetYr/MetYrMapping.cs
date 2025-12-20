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
                WeatherDataYrHourly = Array.Empty<WeatherDataPointYr1h>(),
                WeatherDataYrSixHourly = Array.Empty<WeatherDataPointYr6h>(),
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

                return new WeatherDataPointYr1h
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

        // Map 6-hourly data to WeatherDataPointYr6h[]
        var weatherDataPointYr6h = new List<WeatherDataPointYr6h>();

        foreach (var item in slicedSixHourData)
        {
            try
            {
                // Check if this item has next_6_hours with details (not just summary)
                if (item.Data != null)
                {
                    var next6HoursJson = item.Data.Next6Hours;
                    
                    // Check if details exist (6-hourly forecast) vs just summary (hourly)
                    if (next6HoursJson.TryGetProperty("details", out var detailsElement))
                    {
                        // This is a 6-hour forecast entry
                        var next6Hours = JsonSerializer.Deserialize<MetYrNext6Hours>(next6HoursJson.GetRawText());
                        
                        if (next6Hours != null)
                        {
                            // Parse instant details as 6-hour type
                            var instantDetailsJson = item.Data.Instant.Details;
                            var instant6Hour = JsonSerializer.Deserialize<MetYrInstantDetails6Hour>(instantDetailsJson.GetRawText());
                            
                            if (instant6Hour != null)
                            {
                                weatherDataPointYr6h.Add(new WeatherDataPointYr6h
                                {
                                    Time = item.Time,
                                    AirPressureAtSeaLevel = instant6Hour.AirPressureAtSeaLevel,
                                    AirTemperature = instant6Hour.AirTemperature,
                                    AirTemperaturePercentile10 = instant6Hour.AirTemperaturePercentile10,
                                    AirTemperaturePercentile90 = instant6Hour.AirTemperaturePercentile90,
                                    CloudAreaFraction = instant6Hour.CloudAreaFraction,
                                    CloudAreaFractionHigh = instant6Hour.CloudAreaFractionHigh,
                                    CloudAreaFractionLow = instant6Hour.CloudAreaFractionLow,
                                    CloudAreaFractionMedium = instant6Hour.CloudAreaFractionMedium,
                                    DewPointTemperature = instant6Hour.DewPointTemperature,
                                    RelativeHumidity = instant6Hour.RelativeHumidity,
                                    WindFromDirection = instant6Hour.WindFromDirection,
                                    WindSpeed = instant6Hour.WindSpeed,
                                    PrecipitationAmount = next6Hours.Details.PrecipitationAmount,
                                    PrecipitationAmountMax = next6Hours.Details.PrecipitationAmountMax,
                                    PrecipitationAmountMin = next6Hours.Details.PrecipitationAmountMin,
                                    ProbabilityOfPrecipitation = next6Hours.Details.ProbabilityOfPrecipitation,
                                    SymbolCode = next6Hours.Summary.SymbolCode,
                                    WindSpeedPercentile10 = instant6Hour.WindSpeedPercentile10,
                                    WindSpeedPercentile90 = instant6Hour.WindSpeedPercentile90,
                                    AirTemperatureMax = next6Hours.Details.AirTemperatureMax,
                                    AirTemperatureMin = next6Hours.Details.AirTemperatureMin
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
                // Skip entries that can't be parsed as 6-hour forecasts
                continue;
            }
        }

        return new WeatherDataYr
        {
            WeatherDataYrHourly = weatherDataPointYr1h,
            WeatherDataYrSixHourly = weatherDataPointYr6h,
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


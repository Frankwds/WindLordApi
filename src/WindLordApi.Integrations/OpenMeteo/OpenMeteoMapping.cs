namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Maps OpenMeteo forecast API response data to DTOs.
/// Implements the mapOpenMeteoData logic from Next.js.
/// </summary>
public class OpenMeteoMappingService : IOpenMeteoMapping
{
    private static readonly Dictionary<int, Func<int, string>> MeteoCodeToYrMap = new()
    {
        { 0, isDay => isDay == 1 ? "clearsky_day" : "clearsky_night" }, // Clear sky
        { 1, isDay => isDay == 1 ? "fair_day" : "fair_night" }, // Mainly clear
        { 2, isDay => isDay == 1 ? "partlycloudy_day" : "partlycloudy_night" }, // Partly cloudy
        { 3, _ => "cloudy" }, // Overcast
        { 45, _ => "fog" }, // Fog
        { 48, _ => "fog" }, // Depositing rime fog
        { 51, _ => "lightrain" }, // Light Drizzle
        { 53, _ => "rain" }, // Moderate Drizzle
        { 55, _ => "heavyrain" }, // Dense Drizzle
        { 56, _ => "lightsleet" }, // Light Freezing Drizzle
        { 57, _ => "sleet" }, // Dense Freezing Drizzle
        { 61, _ => "lightrain" }, // Slight Rain
        { 63, _ => "rain" }, // Moderate Rain
        { 65, _ => "heavyrain" }, // Heavy Rain
        { 66, _ => "lightsleet" }, // Light Freezing Rain
        { 67, _ => "sleet" }, // Heavy Freezing Rain
        { 71, _ => "lightsnow" }, // Slight Snow fall
        { 73, _ => "snow" }, // Moderate Snow fall
        { 75, _ => "heavysnow" }, // Heavy Snow fall
        { 77, _ => "snow" }, // Snow grains
        { 80, isDay => isDay == 1 ? "lightrainshowers_day" : "lightrainshowers_night" }, // Slight Rain showers
        { 81, isDay => isDay == 1 ? "rainshowers_day" : "rainshowers_night" }, // Moderate Rain showers
        { 82, isDay => isDay == 1 ? "heavyrainshowers_day" : "heavyrainshowers_night" }, // Violent Rain showers
        { 85, isDay => isDay == 1 ? "lightsnowshowers_day" : "lightsnowshowers_night" }, // Slight Snow showers
        { 86, isDay => isDay == 1 ? "heavysnowshowers_day" : "heavysnowshowers_night" }, // Heavy Snow showers
        { 95, isDay => isDay == 1 ? "lightrainshowersandthunder_day" : "lightrainshowersandthunder_night" }, // Thunderstorm: Slight or moderate
        { 96, isDay => isDay == 1 ? "sleetshowersandthunder_day" : "sleetshowersandthunder_night" }, // Thunderstorm with slight hail
        { 99, isDay => isDay == 1 ? "heavysleetshowersandthunder_day" : "heavysleetshowersandthunder_night" } // Thunderstorm with heavy hail
    };

    /// <summary>
    /// Maps raw API response to WeatherDataPoint array.
    /// Implements the mapOpenMeteoData logic from Next.js.
    /// </summary>
    /// <param name="validatedData">Raw API response from OpenMeteo forecast API.</param>
    /// <returns>Array of WeatherDataPoint DTOs with hourly forecast data.</returns>
    public IReadOnlyList<WeatherDataPoint> MapOpenMeteoData(OpenMeteoResponse validatedData)
    {
        var hourlyData = validatedData.Hourly;
        var timePoints = hourlyData.Time.Count;
        var transformedData = new List<WeatherDataPoint>();

        for (var i = 0; i < timePoints; i++)
        {
            var dataPoint = new WeatherDataPoint
            {
                Time = hourlyData.Time[i],
                WindSpeed1000hPa = hourlyData.WindSpeed1000hPa[i],
                WindDirection1000hPa = hourlyData.WindDirection1000hPa[i],
                WindDirection925hPa = hourlyData.WindDirection925hPa[i],
                WindSpeed925hPa = hourlyData.WindSpeed925hPa[i],
                WindSpeed850hPa = hourlyData.WindSpeed850hPa[i],
                WindDirection850hPa = hourlyData.WindDirection850hPa[i],
                WindDirection700hPa = hourlyData.WindDirection700hPa[i],
                WindSpeed700hPa = hourlyData.WindSpeed700hPa[i],
                Temperature1000hPa = hourlyData.Temperature1000hPa[i],
                Temperature925hPa = hourlyData.Temperature925hPa[i],
                Temperature850hPa = hourlyData.Temperature850hPa[i],
                Temperature700hPa = hourlyData.Temperature700hPa[i],
                Temperature2m = hourlyData.Temperature2m[i],
                Precipitation = hourlyData.Precipitation[i],
                PrecipitationProbability = hourlyData.PrecipitationProbability[i],
                CloudCover = hourlyData.CloudCover[i],
                WindSpeed10m = hourlyData.WindSpeed10m[i],
                WindDirection10m = hourlyData.WindDirection10m[i],
                WindGusts10m = hourlyData.WindGusts10m[i],
                WeatherCode = MapWmoToYrWeatherCode(hourlyData.WeatherCode[i], hourlyData.IsDay[i]),
                PressureMsl = hourlyData.PressureMsl[i],
                ConvectiveInhibition = hourlyData.ConvectiveInhibition[i],
                CloudCoverLow = hourlyData.CloudCoverLow[i],
                CloudCoverMid = hourlyData.CloudCoverMid[i],
                CloudCoverHigh = hourlyData.CloudCoverHigh[i],
                IsDay = hourlyData.IsDay[i],
                FreezingLevelHeight = hourlyData.FreezingLevelHeight[i],
                Cape = hourlyData.Cape[i],
                LiftedIndex = hourlyData.LiftedIndex[i],
                BoundaryLayerHeight = hourlyData.BoundaryLayerHeight[i],
                GeopotentialHeight1000hPa = hourlyData.GeopotentialHeight1000hPa[i],
                GeopotentialHeight925hPa = hourlyData.GeopotentialHeight925hPa[i],
                GeopotentialHeight850hPa = hourlyData.GeopotentialHeight850hPa[i],
                GeopotentialHeight700hPa = hourlyData.GeopotentialHeight700hPa[i]
            };
            transformedData.Add(dataPoint);
        }

        return transformedData;
    }

    /// <summary>
    /// Maps WMO weather code to Yr weather code string.
    /// </summary>
    /// <param name="wmoCode">WMO weather code.</param>
    /// <param name="isDay">Whether it is day (1) or night (0).</param>
    /// <returns>Yr weather code string.</returns>
    public string MapWmoToYrWeatherCode(int wmoCode, int isDay)
    {
        if (MeteoCodeToYrMap.TryGetValue(wmoCode, out var weatherFunc))
        {
            return weatherFunc(isDay);
        }

        return "unknown"; // Default to "unknown" or a suitable fallback if code not found
    }
}


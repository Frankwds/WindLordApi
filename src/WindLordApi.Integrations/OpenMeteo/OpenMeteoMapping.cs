using System.Globalization;

namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Maps Open-Meteo DTOs into worker-facing forecast models.
/// </summary>
public class OpenMeteoMappingService : IOpenMeteoMapping
{
    public IReadOnlyList<OpenMeteoLocationForecast> MapForecasts(IReadOnlyList<OpenMeteoForecastResponse> responses)
    {
        ArgumentNullException.ThrowIfNull(responses);

        return responses.Select(MapLocationForecast).ToArray();
    }

    private static OpenMeteoLocationForecast MapLocationForecast(OpenMeteoForecastResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.Hourly is null)
        {
            throw new InvalidOperationException("Open-Meteo forecast response is missing the hourly payload.");
        }

        var hourly = response.Hourly;
        var timeCount = hourly.Time.Count;

        if (timeCount == 0)
        {
            return new OpenMeteoLocationForecast
            {
                Latitude = response.Latitude,
                Longitude = response.Longitude,
                Forecasts = Array.Empty<OpenMeteoForecastPoint>()
            };
        }

        ValidateAlignedArray(nameof(hourly.Temperature2m), timeCount, hourly.Temperature2m.Count);
        ValidateAlignedArray(nameof(hourly.WindSpeed10m), timeCount, hourly.WindSpeed10m.Count);
        ValidateAlignedArray(nameof(hourly.WindDirection10m), timeCount, hourly.WindDirection10m.Count);
        ValidateAlignedArray(nameof(hourly.Precipitation), timeCount, hourly.Precipitation.Count);
        ValidateAlignedArray(nameof(hourly.PrecipitationProbability), timeCount, hourly.PrecipitationProbability.Count);
        ValidateAlignedArray(nameof(hourly.PressureMsl), timeCount, hourly.PressureMsl.Count);
        ValidateAlignedArray(nameof(hourly.WeatherCode), timeCount, hourly.WeatherCode.Count);
        ValidateAlignedArray(nameof(hourly.IsDay), timeCount, hourly.IsDay.Count);

        var forecasts = new List<OpenMeteoForecastPoint>(timeCount);
        for (var index = 0; index < timeCount; index++)
        {
            var isDay = hourly.IsDay[index] switch
            {
                0 => (short)0,
                1 => (short)1,
                _ => (short?)null
            };

            forecasts.Add(new OpenMeteoForecastPoint
            {
                Time = ParseUtcDateTime(hourly.Time[index]),
                Temperature = RoundDecimal(hourly.Temperature2m[index], 1),
                WindSpeed = RoundDecimal(hourly.WindSpeed10m[index], 1),
                WindDirection = RoundInteger(hourly.WindDirection10m[index]),
                Precipitation = RoundDecimal(hourly.Precipitation[index], 2),
                PrecipitationProbability = RoundFloat(hourly.PrecipitationProbability[index], 2),
                PressureMsl = RoundDecimal(hourly.PressureMsl[index], 1),
                WeatherCode = MapWeatherCode(hourly.WeatherCode[index], isDay),
                IsDay = isDay
            });
        }

        return new OpenMeteoLocationForecast
        {
            Latitude = response.Latitude,
            Longitude = response.Longitude,
            Forecasts = forecasts
        };
    }

    private static void ValidateAlignedArray(string name, int expectedCount, int actualCount)
    {
        if (actualCount != expectedCount)
        {
            throw new InvalidOperationException($"Open-Meteo hourly array '{name}' length {actualCount} did not match the time array length {expectedCount}.");
        }
    }

    private static DateTime ParseUtcDateTime(string input)
    {
        return DateTime.Parse(
            input,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    private static decimal? RoundDecimal(double? value, int decimals)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return Math.Round((decimal)value.Value, decimals, MidpointRounding.AwayFromZero);
    }

    private static float? RoundFloat(double? value, int decimals)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return (float)Math.Round(value.Value, decimals, MidpointRounding.AwayFromZero);
    }

    private static int? RoundInteger(double? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return (int)Math.Round(value.Value, MidpointRounding.AwayFromZero);
    }

    private static string? MapWeatherCode(int? weatherCode, short? isDay)
    {
        return weatherCode switch
        {
            0 => isDay == 0 ? "clearsky_night" : isDay == 1 ? "clearsky_day" : null,
            1 or 2 => isDay == 0 ? "partlycloudy_night" : isDay == 1 ? "partlycloudy_day" : null,
            3 => "cloudy",
            45 or 48 => "fog",
            51 or 53 or 55 => "rain",
            56 or 57 => "sleet",
            61 or 63 or 65 => "rain",
            66 or 67 => "sleet",
            71 or 73 or 75 or 77 => "snow",
            80 or 81 or 82 => "rain",
            85 or 86 => "snow",
            95 or 96 or 99 => "rainandthunder",
            _ => null
        };
    }
}
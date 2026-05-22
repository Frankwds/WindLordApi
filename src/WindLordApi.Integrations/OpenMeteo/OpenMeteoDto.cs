using System.Text.Json.Serialization;

namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// DTO for an Open-Meteo forecast response block.
/// </summary>
public sealed record OpenMeteoForecastResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }

    [JsonPropertyName("hourly")]
    public OpenMeteoHourlyForecast? Hourly { get; init; }
}

/// <summary>
/// DTO for the hourly arrays in an Open-Meteo forecast response block.
/// </summary>
public sealed record OpenMeteoHourlyForecast
{
    [JsonPropertyName("time")]
    public IReadOnlyList<string> Time { get; init; } = Array.Empty<string>();

    [JsonPropertyName("temperature_2m")]
    public IReadOnlyList<double?> Temperature2m { get; init; } = Array.Empty<double?>();

    [JsonPropertyName("wind_speed_10m")]
    public IReadOnlyList<double?> WindSpeed10m { get; init; } = Array.Empty<double?>();

    [JsonPropertyName("wind_direction_10m")]
    public IReadOnlyList<double?> WindDirection10m { get; init; } = Array.Empty<double?>();

    [JsonPropertyName("wind_gusts_10m")]
    public IReadOnlyList<double?> WindGusts10m { get; init; } = Array.Empty<double?>();

    [JsonPropertyName("precipitation")]
    public IReadOnlyList<double?> Precipitation { get; init; } = Array.Empty<double?>();

    [JsonPropertyName("precipitation_probability")]
    public IReadOnlyList<double?> PrecipitationProbability { get; init; } = Array.Empty<double?>();

    [JsonPropertyName("pressure_msl")]
    public IReadOnlyList<double?> PressureMsl { get; init; } = Array.Empty<double?>();

    [JsonPropertyName("weather_code")]
    public IReadOnlyList<int?> WeatherCode { get; init; } = Array.Empty<int?>();

    [JsonPropertyName("is_day")]
    public IReadOnlyList<int?> IsDay { get; init; } = Array.Empty<int?>();
}
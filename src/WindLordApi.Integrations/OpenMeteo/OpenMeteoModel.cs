using System.Text.Json.Serialization;

namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Represents the hourly data from OpenMeteo API.
/// Mirrors the Zod schema: openMeteoResponseSchema.hourly.
/// </summary>
public record OpenMeteoHourly
{
    [JsonRequired]
    [JsonPropertyName("time")]
    public IReadOnlyList<string> Time { get; init; } = Array.Empty<string>();

    [JsonRequired]
    [JsonPropertyName("wind_speed_1000hPa")]
    public IReadOnlyList<decimal> WindSpeed1000hPa { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("wind_direction_1000hPa")]
    public IReadOnlyList<double> WindDirection1000hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("wind_direction_925hPa")]
    public IReadOnlyList<double> WindDirection925hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("wind_speed_925hPa")]
    public IReadOnlyList<decimal> WindSpeed925hPa { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("wind_speed_850hPa")]
    public IReadOnlyList<decimal> WindSpeed850hPa { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("wind_direction_850hPa")]
    public IReadOnlyList<double> WindDirection850hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("wind_direction_700hPa")]
    public IReadOnlyList<double> WindDirection700hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("wind_speed_700hPa")]
    public IReadOnlyList<decimal> WindSpeed700hPa { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("temperature_1000hPa")]
    public IReadOnlyList<decimal> Temperature1000hPa { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("temperature_925hPa")]
    public IReadOnlyList<decimal> Temperature925hPa { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("temperature_850hPa")]
    public IReadOnlyList<decimal> Temperature850hPa { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("temperature_700hPa")]
    public IReadOnlyList<decimal> Temperature700hPa { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("temperature_2m")]
    public IReadOnlyList<decimal> Temperature2m { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("precipitation")]
    public IReadOnlyList<decimal> Precipitation { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("precipitation_probability")]
    public IReadOnlyList<float> PrecipitationProbability { get; init; } = Array.Empty<float>();

    [JsonRequired]
    [JsonPropertyName("cloud_cover")]
    public IReadOnlyList<int> CloudCover { get; init; } = Array.Empty<int>();

    [JsonRequired]
    [JsonPropertyName("wind_speed_10m")]
    public IReadOnlyList<decimal> WindSpeed10m { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("wind_direction_10m")]
    public IReadOnlyList<double> WindDirection10m { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("wind_gusts_10m")]
    public IReadOnlyList<decimal> WindGusts10m { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("weather_code")]
    public IReadOnlyList<int> WeatherCode { get; init; } = Array.Empty<int>();

    [JsonRequired]
    [JsonPropertyName("pressure_msl")]
    public IReadOnlyList<decimal> PressureMsl { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("convective_inhibition")]
    public IReadOnlyList<decimal> ConvectiveInhibition { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("cloud_cover_low")]
    public IReadOnlyList<int> CloudCoverLow { get; init; } = Array.Empty<int>();

    [JsonRequired]
    [JsonPropertyName("cloud_cover_mid")]
    public IReadOnlyList<int> CloudCoverMid { get; init; } = Array.Empty<int>();

    [JsonRequired]
    [JsonPropertyName("cloud_cover_high")]
    public IReadOnlyList<int> CloudCoverHigh { get; init; } = Array.Empty<int>();

    [JsonRequired]
    [JsonPropertyName("is_day")]
    public IReadOnlyList<int> IsDay { get; init; } = Array.Empty<int>();

    [JsonRequired]
    [JsonPropertyName("freezing_level_height")]
    public IReadOnlyList<decimal> FreezingLevelHeight { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("cape")]
    public IReadOnlyList<decimal> Cape { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("lifted_index")]
    public IReadOnlyList<decimal> LiftedIndex { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("boundary_layer_height")]
    public IReadOnlyList<decimal> BoundaryLayerHeight { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("geopotential_height_1000hPa")]
    public IReadOnlyList<decimal> GeopotentialHeight1000hPa { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("geopotential_height_925hPa")]
    public IReadOnlyList<decimal> GeopotentialHeight925hPa { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("geopotential_height_850hPa")]
    public IReadOnlyList<decimal> GeopotentialHeight850hPa { get; init; } = Array.Empty<decimal>();

    [JsonRequired]
    [JsonPropertyName("geopotential_height_700hPa")]
    public IReadOnlyList<decimal> GeopotentialHeight700hPa { get; init; } = Array.Empty<decimal>();
}

/// <summary>
/// Root response model from OpenMeteo API.
/// Mirrors the Zod schema: openMeteoResponseSchema.
/// </summary>
public record OpenMeteoResponse
{
    [JsonRequired]
    [JsonPropertyName("elevation")]
    public double Elevation { get; init; }

    [JsonRequired]
    [JsonPropertyName("hourly")]
    public OpenMeteoHourly Hourly { get; init; } = null!;
}


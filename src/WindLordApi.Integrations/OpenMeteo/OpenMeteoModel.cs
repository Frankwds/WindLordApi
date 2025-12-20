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
    public IReadOnlyList<double> WindSpeed1000hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("wind_direction_1000hPa")]
    public IReadOnlyList<double> WindDirection1000hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("wind_direction_925hPa")]
    public IReadOnlyList<double> WindDirection925hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("wind_speed_925hPa")]
    public IReadOnlyList<double> WindSpeed925hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("wind_speed_850hPa")]
    public IReadOnlyList<double> WindSpeed850hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("wind_direction_850hPa")]
    public IReadOnlyList<double> WindDirection850hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("wind_direction_700hPa")]
    public IReadOnlyList<double> WindDirection700hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("wind_speed_700hPa")]
    public IReadOnlyList<double> WindSpeed700hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("temperature_1000hPa")]
    public IReadOnlyList<double> Temperature1000hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("temperature_925hPa")]
    public IReadOnlyList<double> Temperature925hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("temperature_850hPa")]
    public IReadOnlyList<double> Temperature850hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("temperature_700hPa")]
    public IReadOnlyList<double> Temperature700hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("temperature_2m")]
    public IReadOnlyList<double> Temperature2m { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("precipitation")]
    public IReadOnlyList<double> Precipitation { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("precipitation_probability")]
    public IReadOnlyList<double> PrecipitationProbability { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("cloud_cover")]
    public IReadOnlyList<double> CloudCover { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("wind_speed_10m")]
    public IReadOnlyList<double> WindSpeed10m { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("wind_direction_10m")]
    public IReadOnlyList<double> WindDirection10m { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("wind_gusts_10m")]
    public IReadOnlyList<double> WindGusts10m { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("weather_code")]
    public IReadOnlyList<int> WeatherCode { get; init; } = Array.Empty<int>();

    [JsonRequired]
    [JsonPropertyName("pressure_msl")]
    public IReadOnlyList<double> PressureMsl { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("convective_inhibition")]
    public IReadOnlyList<double> ConvectiveInhibition { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("cloud_cover_low")]
    public IReadOnlyList<double> CloudCoverLow { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("cloud_cover_mid")]
    public IReadOnlyList<double> CloudCoverMid { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("cloud_cover_high")]
    public IReadOnlyList<double> CloudCoverHigh { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("is_day")]
    public IReadOnlyList<int> IsDay { get; init; } = Array.Empty<int>();

    [JsonRequired]
    [JsonPropertyName("freezing_level_height")]
    public IReadOnlyList<double> FreezingLevelHeight { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("cape")]
    public IReadOnlyList<double> Cape { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("lifted_index")]
    public IReadOnlyList<double> LiftedIndex { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("boundary_layer_height")]
    public IReadOnlyList<double> BoundaryLayerHeight { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("geopotential_height_1000hPa")]
    public IReadOnlyList<double> GeopotentialHeight1000hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("geopotential_height_925hPa")]
    public IReadOnlyList<double> GeopotentialHeight925hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("geopotential_height_850hPa")]
    public IReadOnlyList<double> GeopotentialHeight850hPa { get; init; } = Array.Empty<double>();

    [JsonRequired]
    [JsonPropertyName("geopotential_height_700hPa")]
    public IReadOnlyList<double> GeopotentialHeight700hPa { get; init; } = Array.Empty<double>();
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


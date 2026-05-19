using System.Text.Json.Serialization;
using WindLordApi.Data.Models;

namespace WindLordApi.Integrations.PortWind;

/// <summary>
/// PortWind station catalog entry extracted from the JavaScript-wrapped payload.
/// </summary>
public record PortWindStationCatalogEntry
{
    [JsonPropertyName("status")]
    public bool? Status { get; init; }

    [JsonPropertyName("history")]
    public bool? History { get; init; }

    [JsonPropertyName("label")]
    public string? Label { get; init; }

    [JsonPropertyName("location")]
    public PortWindStationLocation? Location { get; init; }
}

/// <summary>
/// PortWind station coordinates.
/// </summary>
public record PortWindStationLocation
{
    [JsonPropertyName("lat")]
    public decimal? Latitude { get; init; }

    [JsonPropertyName("lng")]
    public decimal? Longitude { get; init; }
}

/// <summary>
/// PortWind latest observation response.
/// </summary>
public record PortWindLatestResponse
{
    [JsonPropertyName("server_time")]
    public long? ServerTime { get; init; }

    [JsonPropertyName("last_measurement")]
    public long? LastMeasurement { get; init; }

    [JsonPropertyName("data")]
    public IReadOnlyList<PortWindLatestDataPoint> Data { get; init; } = Array.Empty<PortWindLatestDataPoint>();
}

/// <summary>
/// PortWind latest observation data point.
/// </summary>
public record PortWindLatestDataPoint
{
    [JsonPropertyName("uts")]
    public long? Timestamp { get; init; }

    [JsonPropertyName("temperature_avg")]
    public decimal? TemperatureAverage { get; init; }

    [JsonPropertyName("wind_direction_avg")]
    public decimal? WindDirectionAverage { get; init; }

    [JsonPropertyName("wind_speed_avg")]
    public decimal? WindSpeedAverage { get; init; }

    [JsonPropertyName("wind_speed_max")]
    public decimal? WindSpeedMax { get; init; }

    [JsonPropertyName("wind_gust")]
    public decimal? WindGust { get; init; }
}

/// <summary>
/// Result of mapping a provider station catalog into persistence operations.
/// </summary>
public record PortWindStationRefreshResult
{
    public IReadOnlyList<WeatherStation> WeatherStations { get; init; } = Array.Empty<WeatherStation>();

    public IReadOnlyList<string> SeenStationIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> ActiveStationIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> InactiveStationIds { get; init; } = Array.Empty<string>();
}
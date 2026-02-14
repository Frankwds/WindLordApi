using System.Text.Json.Serialization;

namespace WindLordApi.Integrations.WindsMobi;

/// <summary>
/// Represents the "last" (latest observation) data from a WindsMobi station.
/// </summary>
public record WindsMobiLastData
{
    /// <summary>
    /// Unix timestamp (seconds since epoch) of the observation.
    /// </summary>
    [JsonPropertyName("_id")]
    public long? Id { get; init; }

    /// <summary>
    /// Wind direction in degrees (0-360).
    /// </summary>
    [JsonPropertyName("w-dir")]
    public int? WindDirection { get; init; }

    /// <summary>
    /// Average wind speed in m/s.
    /// </summary>
    [JsonPropertyName("w-avg")]
    public decimal? WindAverage { get; init; }

    /// <summary>
    /// Maximum wind speed (gust) in m/s.
    /// </summary>
    [JsonPropertyName("w-max")]
    public decimal? WindMax { get; init; }

    /// <summary>
    /// Temperature in degrees Celsius.
    /// </summary>
    [JsonPropertyName("temp")]
    public decimal? Temperature { get; init; }
}

/// <summary>
/// Represents the GeoJSON location from a WindsMobi station.
/// Coordinates are in GeoJSON order: [longitude, latitude].
/// </summary>
public record WindsMobiLocation
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// GeoJSON coordinates: [longitude, latitude].
    /// </summary>
    [JsonPropertyName("coordinates")]
    public decimal[]? Coordinates { get; init; }
}

/// <summary>
/// Represents a single station from the WindsMobi API response.
/// </summary>
public record WindsMobiStation
{
    /// <summary>
    /// Unique station identifier (e.g. "zermatt-blauherd-schneianlage").
    /// </summary>
    [JsonPropertyName("_id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Altitude in meters.
    /// </summary>
    [JsonPropertyName("alt")]
    public int? Altitude { get; init; }

    /// <summary>
    /// GeoJSON location with coordinates [longitude, latitude].
    /// </summary>
    [JsonPropertyName("loc")]
    public WindsMobiLocation? Location { get; init; }

    /// <summary>
    /// Provider code (e.g. "zermatt").
    /// </summary>
    [JsonPropertyName("pv-code")]
    public string? ProviderCode { get; init; }

    /// <summary>
    /// Short/display name of the station (e.g. "Blauherd").
    /// </summary>
    [JsonPropertyName("short")]
    public string? ShortName { get; init; }

    /// <summary>
    /// Latest observation data.
    /// </summary>
    [JsonPropertyName("last")]
    public WindsMobiLastData? Last { get; init; }
}

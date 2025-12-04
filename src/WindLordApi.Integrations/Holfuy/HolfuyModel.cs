using System.Text.Json.Serialization;

namespace WindLordApi.Integrations.Holfuy;

/// <summary>
/// Represents wind data from Holfuy API.
/// Mirrors the Zod schema: windDataSchema.
/// </summary>
public record HolfuyWindData
{
    [JsonRequired]
    [JsonPropertyName("speed")]
    public double Speed { get; init; }

    [JsonRequired]
    [JsonPropertyName("gust")]
    public double Gust { get; init; }

    [JsonRequired]
    [JsonPropertyName("min")]
    public double Min { get; init; }

    [JsonRequired]
    [JsonPropertyName("unit")]
    public string Unit { get; init; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("direction")]
    public double Direction { get; init; }
}

/// <summary>
/// Represents location data from Holfuy API.
/// Mirrors the Zod schema location object.
/// </summary>
public record HolfuyLocation
{
    [JsonRequired]
    [JsonPropertyName("latitude")]
    public string Latitude { get; init; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("longitude")]
    public string Longitude { get; init; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("altitude")]
    public double Altitude { get; init; }
}

/// <summary>
/// Represents a single Holfuy weather station data point.
/// Mirrors the Zod schema: holfuyStationDataSchema.
/// </summary>
public record HolfuyStationData
{
    [JsonRequired]
    [JsonPropertyName("stationId")]
    public int StationId { get; init; }

    [JsonRequired]
    [JsonPropertyName("stationName")]
    public string StationName { get; init; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("location")]
    public HolfuyLocation Location { get; init; } = null!;

    [JsonRequired]
    [JsonPropertyName("dateTime")]
    public string DateTime { get; init; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("wind")]
    public HolfuyWindData Wind { get; init; } = null!;

    [JsonPropertyName("humidity")]
    public double? Humidity { get; init; }

    [JsonPropertyName("pressure")]
    public double? Pressure { get; init; }

    [JsonPropertyName("rain")]
    public double? Rain { get; init; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; init; }
}

// Note: The Holfuy API response is directly an array of HolfuyStationData
// No wrapper object needed - deserialize directly to List<HolfuyStationData>


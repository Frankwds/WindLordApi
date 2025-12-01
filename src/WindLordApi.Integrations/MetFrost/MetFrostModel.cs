using System.Text.Json.Serialization;

namespace WindLordApi.Integrations.MetFrost;

/// <summary>
/// Represents the level information for a MET observation.
/// Mirrors the Zod schema: metObservationLevelSchema.
/// </summary>
public record MetObservationLevel
{
    [JsonPropertyName("levelType")]
    public string LevelType { get; init; } = string.Empty;

    [JsonPropertyName("unit")]
    public string Unit { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public double Value { get; init; }
}

/// <summary>
/// Represents a single MET observation element.
/// Mirrors the Zod schema: metObservationSchema.
/// </summary>
public record MetObservation
{
    [JsonPropertyName("elementId")]
    public string ElementId { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public double Value { get; init; }

    /// <summary>
    /// Unit of the observation value, may be null or missing.
    /// </summary>
    [JsonPropertyName("unit")]
    public string? Unit { get; init; }

    /// <summary>
    /// Optional level information for the observation.
    /// </summary>
    [JsonPropertyName("level")]
    public MetObservationLevel? Level { get; init; }

    [JsonPropertyName("timeOffset")]
    public string? TimeOffset { get; init; }

    [JsonPropertyName("timeResolution")]
    public string? TimeResolution { get; init; }

    [JsonPropertyName("timeSeriesId")]
    public int? TimeSeriesId { get; init; }

    [JsonPropertyName("performanceCategory")]
    public string? PerformanceCategory { get; init; }

    [JsonPropertyName("exposureCategory")]
    public string? ExposureCategory { get; init; }

    [JsonPropertyName("qualityCode")]
    public int? QualityCode { get; init; }
}

/// <summary>
/// Represents the data block for a MET observations response.
/// Mirrors the Zod schema: metObservationsDataSchema.
/// </summary>
public record MetObservationsData
{
    [JsonPropertyName("sourceId")]
    public string SourceId { get; init; } = string.Empty;

    /// <summary>
    /// Reference time of the observations (ISO datetime).
    /// </summary>
    [JsonPropertyName("referenceTime")]
    public DateTimeOffset ReferenceTime { get; init; }

    [JsonPropertyName("observations")]
    public IReadOnlyList<MetObservation> Observations { get; init; } = Array.Empty<MetObservation>();
}

/// <summary>
/// Root model for MET Frost observations API response.
/// Mirrors the Zod schema: metObservationsResponseSchema.
/// </summary>
public record MetObservationsResponse
{
    /// <summary>
    /// JSON-LD context URL.
    /// </summary>
    [JsonPropertyName("@context")]
    public string Context { get; init; } = string.Empty;

    [JsonPropertyName("@type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; init; } = string.Empty;

    [JsonPropertyName("license")]
    public Uri License { get; init; } = new("https://example.com");

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("queryTime")]
    public double QueryTime { get; init; }

    [JsonPropertyName("currentItemCount")]
    public int CurrentItemCount { get; init; }

    [JsonPropertyName("itemsPerPage")]
    public int ItemsPerPage { get; init; }

    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    [JsonPropertyName("totalItemCount")]
    public int TotalItemCount { get; init; }

    /// <summary>
    /// Optional link to the current page.
    /// </summary>
    [JsonPropertyName("currentLink")]
    public Uri? CurrentLink { get; init; }

    [JsonPropertyName("data")]
    public IReadOnlyList<MetObservationsData> Data { get; init; } = Array.Empty<MetObservationsData>();
}



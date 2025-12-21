using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindLordApi.Integrations.MetYr;

/// <summary>
/// Represents the geometry information from MET.no Locationforecast API.
/// Mirrors the Zod schema: metNoResponseSchema.geometry.
/// </summary>
public record MetYrGeometry
{
    [JsonRequired]
    [JsonPropertyName("coordinates")]
    public double[] Coordinates { get; init; } = Array.Empty<double>();
}

/// <summary>
/// Represents the meta information from MET.no Locationforecast API.
/// Mirrors the Zod schema: metNoResponseSchema.properties.meta.
/// </summary>
public record MetYrMeta
{
    [JsonRequired]
    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; init; } = string.Empty;
}

/// <summary>
/// Base instant details schema shared between 1-hour and 6-hour forecasts.
/// Mirrors the Zod schema: BaseInstantDetailsSchema.
/// </summary>
public record MetYrBaseInstantDetails
{
    [JsonRequired]
    [JsonPropertyName("air_pressure_at_sea_level")]
    public decimal AirPressureAtSeaLevel { get; init; }

    [JsonRequired]
    [JsonPropertyName("air_temperature")]
    public decimal AirTemperature { get; init; }

    [JsonPropertyName("air_temperature_percentile_10")]
    public decimal? AirTemperaturePercentile10 { get; init; }

    [JsonPropertyName("air_temperature_percentile_90")]
    public decimal? AirTemperaturePercentile90 { get; init; }

    [JsonRequired]
    [JsonPropertyName("cloud_area_fraction")]
    public double CloudAreaFraction { get; init; }

    [JsonRequired]
    [JsonPropertyName("cloud_area_fraction_high")]
    public double CloudAreaFractionHigh { get; init; }

    [JsonRequired]
    [JsonPropertyName("cloud_area_fraction_low")]
    public double CloudAreaFractionLow { get; init; }

    [JsonRequired]
    [JsonPropertyName("cloud_area_fraction_medium")]
    public double CloudAreaFractionMedium { get; init; }

    [JsonRequired]
    [JsonPropertyName("dew_point_temperature")]
    public decimal DewPointTemperature { get; init; }

    [JsonRequired]
    [JsonPropertyName("relative_humidity")]
    public double RelativeHumidity { get; init; }

    [JsonRequired]
    [JsonPropertyName("wind_from_direction")]
    public double WindFromDirection { get; init; }

    [JsonRequired]
    [JsonPropertyName("wind_speed")]
    public decimal WindSpeed { get; init; }
}

/// <summary>
/// Instant details for 1-hour forecast.
/// Mirrors the Zod schema: InstantDetailsSchema1Hour.
/// </summary>
public record MetYrInstantDetails1Hour : MetYrBaseInstantDetails
{
    [JsonRequired]
    [JsonPropertyName("fog_area_fraction")]
    public double FogAreaFraction { get; init; }

    [JsonRequired]
    [JsonPropertyName("ultraviolet_index_clear_sky")]
    public double UltravioletIndexClearSky { get; init; }

    [JsonPropertyName("wind_speed_of_gust")]
    public decimal? WindSpeedOfGust { get; init; }
}

/// <summary>
/// Instant details for 6-hour forecast.
/// Mirrors the Zod schema: InstantDetailsSchema6Hour.
/// </summary>
public record MetYrInstantDetails6Hour : MetYrBaseInstantDetails
{
    [JsonPropertyName("wind_speed_percentile_10")]
    public double? WindSpeedPercentile10 { get; init; }

    [JsonPropertyName("wind_speed_percentile_90")]
    public double? WindSpeedPercentile90 { get; init; }
}

/// <summary>
/// Base next hours details schema shared between 1-hour and 6-hour forecasts.
/// Mirrors the Zod schema: BaseNextHoursDetailsSchema.
/// </summary>
public record MetYrBaseNextHoursDetails
{
    [JsonRequired]
    [JsonPropertyName("precipitation_amount")]
    public decimal PrecipitationAmount { get; init; }

    [JsonPropertyName("precipitation_amount_max")]
    public double? PrecipitationAmountMax { get; init; }

    [JsonPropertyName("precipitation_amount_min")]
    public double? PrecipitationAmountMin { get; init; }

    [JsonPropertyName("probability_of_precipitation")]
    public float? ProbabilityOfPrecipitation { get; init; }
}

/// <summary>
/// Next 1 hours details.
/// Mirrors the Zod schema: Next1HoursDetailsSchema.
/// </summary>
public record MetYrNext1HoursDetails : MetYrBaseNextHoursDetails
{
    [JsonPropertyName("probability_of_thunder")]
    public double? ProbabilityOfThunder { get; init; }
}

/// <summary>
/// Next 6 hours details.
/// Mirrors the Zod schema: Next6HoursDetailsSchema.
/// </summary>
public record MetYrNext6HoursDetails : MetYrBaseNextHoursDetails
{
    [JsonRequired]
    [JsonPropertyName("air_temperature_max")]
    public decimal AirTemperatureMax { get; init; }

    [JsonRequired]
    [JsonPropertyName("air_temperature_min")]
    public decimal AirTemperatureMin { get; init; }
}

/// <summary>
/// Summary with symbol code.
/// Mirrors the Zod schema: summary object.
/// </summary>
public record MetYrSummary
{
    [JsonRequired]
    [JsonPropertyName("symbol_code")]
    public string SymbolCode { get; init; } = string.Empty;
}

/// <summary>
/// Next 1 hours schema.
/// Mirrors the Zod schema: Next1HoursSchema.
/// </summary>
public record MetYrNext1Hours
{
    [JsonRequired]
    [JsonPropertyName("summary")]
    public MetYrSummary Summary { get; init; } = null!;

    [JsonRequired]
    [JsonPropertyName("details")]
    public MetYrNext1HoursDetails Details { get; init; } = null!;
}

/// <summary>
/// Next 6 hours schema.
/// Mirrors the Zod schema: Next6HoursSchema.
/// </summary>
public record MetYrNext6Hours
{
    [JsonRequired]
    [JsonPropertyName("summary")]
    public MetYrSummary Summary { get; init; } = null!;

    [JsonRequired]
    [JsonPropertyName("details")]
    public MetYrNext6HoursDetails Details { get; init; } = null!;
}

/// <summary>
/// Next 6 hours schema for hourly forecast (summary only).
/// Mirrors the Zod schema: Next6HoursSchemaForHourly.
/// </summary>
public record MetYrNext6HoursForHourly
{
    [JsonRequired]
    [JsonPropertyName("summary")]
    public MetYrSummary Summary { get; init; } = null!;
}

/// <summary>
/// Instant data wrapper with flexible details that can represent both 1-hour and 6-hour instant data.
/// </summary>
public record MetYrInstant
{
    [JsonRequired]
    [JsonPropertyName("details")]
    public JsonElement Details { get; init; }
}

/// <summary>
/// Instant data wrapper for 1-hour forecast.
/// </summary>
public record MetYrInstant1Hour
{
    [JsonRequired]
    [JsonPropertyName("details")]
    public MetYrInstantDetails1Hour Details { get; init; } = null!;
}

/// <summary>
/// Instant data wrapper for 6-hour forecast.
/// </summary>
public record MetYrInstant6Hour
{
    [JsonRequired]
    [JsonPropertyName("details")]
    public MetYrInstantDetails6Hour Details { get; init; } = null!;
}

/// <summary>
/// Flexible time series data that can represent both 1-hour and 6-hour forecasts.
/// The API returns a single timeseries array where entries may have next_1_hours (hourly)
/// or only next_6_hours with details (6-hourly).
/// </summary>
public record MetYrTimeSeriesData
{
    [JsonRequired]
    [JsonPropertyName("instant")]
    public MetYrInstant Instant { get; init; } = null!;

    /// <summary>
    /// Present in hourly forecast entries.
    /// </summary>
    [JsonPropertyName("next_1_hours")]
    public MetYrNext1Hours? Next1Hours { get; init; }

    /// <summary>
    /// Present in both hourly (summary only) and 6-hourly (with details) entries.
    /// Stored as JsonElement to handle both structures flexibly.
    /// May be absent in some timeseries entries.
    /// </summary>
    [JsonPropertyName("next_6_hours")]
    public JsonElement Next6Hours { get; init; }
}

/// <summary>
/// Time series entry from the API.
/// The API returns a single timeseries array with entries that may be hourly or 6-hourly.
/// </summary>
public record MetYrTimeSeries
{
    [JsonRequired]
    [JsonPropertyName("time")]
    public string Time { get; init; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("data")]
    public MetYrTimeSeriesData Data { get; init; } = null!;
}

/// <summary>
/// Properties from MET.no Locationforecast API.
/// Mirrors the Zod schema: metNoResponseSchema.properties.
/// </summary>
public record MetYrProperties
{
    [JsonRequired]
    [JsonPropertyName("meta")]
    public MetYrMeta Meta { get; init; } = null!;

    [JsonRequired]
    [JsonPropertyName("timeseries")]
    public IReadOnlyList<MetYrTimeSeries> Timeseries { get; init; } = Array.Empty<MetYrTimeSeries>();
}

/// <summary>
/// Root response model from MET.no Locationforecast API.
/// Mirrors the Zod schema: metNoResponseSchema.
/// </summary>
public record MetYrResponse
{
    [JsonRequired]
    [JsonPropertyName("geometry")]
    public MetYrGeometry Geometry { get; init; } = null!;

    [JsonRequired]
    [JsonPropertyName("properties")]
    public MetYrProperties Properties { get; init; } = null!;
}


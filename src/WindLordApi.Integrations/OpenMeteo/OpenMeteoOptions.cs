namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Configuration options for OpenMeteo forecast API client.
/// </summary>
public class OpenMeteoOptions
{
    public const string SectionName = "OpenMeteo";

    /// <summary>
    /// Base URL for the OpenMeteo forecast API.
    /// Defaults to "https://api.open-meteo.com/v1/forecast" if not specified.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.open-meteo.com/v1/forecast";

    /// <summary>
    /// Wind speed unit.
    /// Defaults to "ms" (meters per second).
    /// </summary>
    public string WindSpeedUnit { get; set; } = "ms";

    /// <summary>
    /// Number of forecast days.
    /// Defaults to "3".
    /// </summary>
    public string ForecastDays { get; set; } = "3";

    /// <summary>
    /// Weather model to use.
    /// Defaults to "best_match".
    /// </summary>
    public string Models { get; set; } = "best_match";

    /// <summary>
    /// Timezone for the forecast data.
    /// Defaults to "GMT".
    /// </summary>
    public string Timezone { get; set; } = "GMT";

    /// <summary>
    /// List of hourly parameters to request from the API.
    /// Defaults to all parameters from the API_URL_CONFIG.
    /// </summary>
    public IReadOnlyList<string> HourlyParameters { get; set; } = new[]
    {
        "wind_speed_1000hPa",
        "wind_direction_1000hPa",
        "wind_direction_925hPa",
        "wind_speed_925hPa",
        "wind_speed_850hPa",
        "wind_direction_850hPa",
        "wind_direction_700hPa",
        "wind_speed_700hPa",
        "temperature_1000hPa",
        "temperature_925hPa",
        "temperature_850hPa",
        "temperature_700hPa",
        "temperature_2m",
        "precipitation",
        "precipitation_probability",
        "cloud_cover",
        "wind_speed_10m",
        "wind_direction_10m",
        "wind_gusts_10m",
        "weather_code",
        "pressure_msl",
        "convective_inhibition",
        "cloud_cover_low",
        "cloud_cover_mid",
        "cloud_cover_high",
        "is_day",
        "freezing_level_height",
        "cape",
        "lifted_index",
        "boundary_layer_height",
        "geopotential_height_1000hPa",
        "geopotential_height_925hPa",
        "geopotential_height_850hPa",
        "geopotential_height_700hPa"
    };
}


namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Request coordinates for an Open-Meteo batch forecast.
/// </summary>
public sealed record OpenMeteoRequestLocation(
    double Latitude,
    double Longitude);

/// <summary>
/// Mapped Open-Meteo forecast rows for a single location block.
/// </summary>
public sealed record OpenMeteoLocationForecast
{
    public required double Latitude { get; init; }

    public required double Longitude { get; init; }

    public required IReadOnlyList<OpenMeteoForecastPoint> Forecasts { get; init; }
}

/// <summary>
/// A single mapped Open-Meteo forecast point.
/// </summary>
public sealed record OpenMeteoForecastPoint
{
    public required DateTime Time { get; init; }

    public decimal? Temperature { get; init; }

    public decimal? WindSpeed { get; init; }

    public int? WindDirection { get; init; }

    public decimal? WindGusts { get; init; }

    public decimal? Precipitation { get; init; }

    public float? PrecipitationProbability { get; init; }

    public decimal? PressureMsl { get; init; }

    public string? WeatherCode { get; init; }

    public short? IsDay { get; init; }
}
namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Weather data point from OpenMeteo forecast.
/// Mirrors the TypeScript interface: WeatherDataPoint.
/// </summary>
public record WeatherDataPoint
{
    // Basic identification
    public required string Time { get; init; }

    // Surface conditions
    public required double Temperature2m { get; init; }
    public required double WindSpeed10m { get; init; }
    public required double WindDirection10m { get; init; }
    public required double WindGusts10m { get; init; }
    public required double Precipitation { get; init; }
    public required double PrecipitationProbability { get; init; }
    public required double PressureMsl { get; init; }
    public required string WeatherCode { get; init; }
    public required int IsDay { get; init; }

    // Atmospheric conditions - Wind at different pressure levels
    public required double WindSpeed1000hPa { get; init; }
    public required double WindDirection1000hPa { get; init; }
    public required double WindSpeed925hPa { get; init; }
    public required double WindDirection925hPa { get; init; }
    public required double WindSpeed850hPa { get; init; }
    public required double WindDirection850hPa { get; init; }
    public required double WindSpeed700hPa { get; init; }
    public required double WindDirection700hPa { get; init; }

    // Atmospheric conditions - Temperature at different pressure levels
    public required double Temperature1000hPa { get; init; }
    public required double Temperature925hPa { get; init; }
    public required double Temperature850hPa { get; init; }
    public required double Temperature700hPa { get; init; }

    // Atmospheric conditions - Cloud cover
    public required double CloudCover { get; init; }
    public required double CloudCoverLow { get; init; }
    public required double CloudCoverMid { get; init; }
    public required double CloudCoverHigh { get; init; }

    // Atmospheric conditions - Stability and convection
    public required double Cape { get; init; }
    public required double ConvectiveInhibition { get; init; }
    public required double LiftedIndex { get; init; }
    public required double BoundaryLayerHeight { get; init; }
    public required double FreezingLevelHeight { get; init; }

    // Atmospheric conditions - Geopotential heights
    public required double GeopotentialHeight1000hPa { get; init; }
    public required double GeopotentialHeight925hPa { get; init; }
    public required double GeopotentialHeight850hPa { get; init; }
    public required double GeopotentialHeight700hPa { get; init; }
}


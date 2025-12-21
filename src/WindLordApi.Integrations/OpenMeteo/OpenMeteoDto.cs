namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Weather data point from OpenMeteo forecast.
/// Mirrors the TypeScript interface: WeatherDataPoint.
/// </summary>
public record OpenMeteoDto
{
    // Basic identification
    public required string Time { get; init; }

    // Surface conditions
    public required decimal Temperature2m { get; init; }
    public required decimal WindSpeed10m { get; init; }
    public required double WindDirection10m { get; init; }
    public required decimal WindGusts10m { get; init; }
    public required decimal Precipitation { get; init; }
    public required float PrecipitationProbability { get; init; }
    public required decimal PressureMsl { get; init; }
    public required string WeatherCode { get; init; }
    public required int IsDay { get; init; }

    // Atmospheric conditions - Wind at different pressure levels
    public required decimal WindSpeed1000hPa { get; init; }
    public required double WindDirection1000hPa { get; init; }
    public required decimal WindSpeed925hPa { get; init; }
    public required double WindDirection925hPa { get; init; }
    public required decimal WindSpeed850hPa { get; init; }
    public required double WindDirection850hPa { get; init; }
    public required decimal WindSpeed700hPa { get; init; }
    public required double WindDirection700hPa { get; init; }

    // Atmospheric conditions - Temperature at different pressure levels
    public required decimal Temperature1000hPa { get; init; }
    public required decimal Temperature925hPa { get; init; }
    public required decimal Temperature850hPa { get; init; }
    public required decimal Temperature700hPa { get; init; }

    // Atmospheric conditions - Cloud cover
    public required int CloudCover { get; init; }
    public required int CloudCoverLow { get; init; }
    public required int CloudCoverMid { get; init; }
    public required int CloudCoverHigh { get; init; }

    // Atmospheric conditions - Stability and convection
    public required decimal Cape { get; init; }
    public required decimal ConvectiveInhibition { get; init; }
    public required decimal LiftedIndex { get; init; }
    public required decimal BoundaryLayerHeight { get; init; }
    public required decimal FreezingLevelHeight { get; init; }

    // Atmospheric conditions - Geopotential heights
    public required decimal GeopotentialHeight1000hPa { get; init; }
    public required decimal GeopotentialHeight925hPa { get; init; }
    public required decimal GeopotentialHeight850hPa { get; init; }
    public required decimal GeopotentialHeight700hPa { get; init; }
}


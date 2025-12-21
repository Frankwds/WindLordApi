namespace WindLordApi.Data.Models;

/// <summary>
/// Combined hourly forecast data from OpenMeteo and MetYr APIs.
/// Mirrors the TypeScript interface: ForecastCache.
/// </summary>
public record ForecastCache
{
    // Basic identification
    public required string Time { get; init; }
    public string? UpdatedAt { get; init; }
    public required string LocationId { get; init; }

    // Surface conditions
    public required double Temperature { get; init; }
    public required double WindSpeed { get; init; }
    public required int WindDirection { get; init; }
    public double? WindGusts { get; init; }
    public required double Precipitation { get; init; }
    public double? PrecipitationMax { get; init; }
    public double? PrecipitationMin { get; init; }
    public double? PrecipitationProbability { get; init; }
    public required double PressureMsl { get; init; }
    public required string WeatherCode { get; init; }
    public required int IsDay { get; init; } // 0 or 1
    public required bool IsYrData { get; init; }


    // Landing conditions
    public double? LandingWind { get; init; }
    public double? LandingGust { get; init; }
    public int? LandingWindDirection { get; init; }

    // Atmospheric conditions - Wind at different pressure levels
    public required double WindSpeed1000hpa { get; init; }
    public required int WindDirection1000hpa { get; init; }
    public required double WindSpeed925hpa { get; init; }
    public required int WindDirection925hpa { get; init; }
    public required double WindSpeed850hpa { get; init; }
    public required int WindDirection850hpa { get; init; }
    public required double WindSpeed700hpa { get; init; }
    public required int WindDirection700hpa { get; init; }

    // Atmospheric conditions - Temperature at different pressure levels
    public required double Temperature1000hpa { get; init; }
    public required double Temperature925hpa { get; init; }
    public required double Temperature850hpa { get; init; }
    public required double Temperature700hpa { get; init; }

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
    public required double GeopotentialHeight1000hpa { get; init; }
    public required double GeopotentialHeight925hpa { get; init; }
    public required double GeopotentialHeight850hpa { get; init; }
    public required double GeopotentialHeight700hpa { get; init; }
}


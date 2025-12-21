namespace WindLordApi.Integrations.MetYr;

/// <summary>
/// Weather data point from Met.no forecast.
/// Mirrors the TypeScript interface: MetYrDto.
/// </summary>
public record MetYrDto
{
    public required string Time { get; init; }
    public required decimal AirPressureAtSeaLevel { get; init; }
    public required decimal AirTemperature { get; init; }
    public decimal? AirTemperaturePercentile10 { get; init; }
    public decimal? AirTemperaturePercentile90 { get; init; }
    public required double CloudAreaFraction { get; init; }
    public required double CloudAreaFractionHigh { get; init; }
    public required double CloudAreaFractionLow { get; init; }
    public required double CloudAreaFractionMedium { get; init; }
    public required decimal DewPointTemperature { get; init; }
    public required double RelativeHumidity { get; init; }
    public required double WindFromDirection { get; init; }
    public required decimal WindSpeed { get; init; }
    public required decimal PrecipitationAmount { get; init; }
    public double? PrecipitationAmountMax { get; init; }
    public double? PrecipitationAmountMin { get; init; }
    public float? ProbabilityOfPrecipitation { get; init; }
    public required string SymbolCode { get; init; }
    public required double FogAreaFraction { get; init; }
    public required double UltravioletIndexClearSky { get; init; }
    public decimal? WindSpeedOfGust { get; init; }
    public double? ProbabilityOfThunder { get; init; }
    public required string Next6HoursSymbolCode { get; init; }
}

/// <summary>
/// Location information.
/// </summary>
public record LocationInfo
{
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
}

/// <summary>
/// Complete weather data from Yr forecast API.
/// Mirrors the TypeScript interface: WeatherDataYr.
/// </summary>
public record WeatherDataYr
{
    public required IReadOnlyList<MetYrDto> MetYrDto { get; init; }
    public required string UpdatedAt { get; init; }
    public required double Elevation { get; init; }
    public required LocationInfo Location { get; init; }
}


using WindLordApi.Data.Models;
using WindLordApi.Integrations.MetYr;
using WindLordApi.Integrations.OpenMeteo;

namespace WindLordApi.Tests.Helpers;

/// <summary>
/// Builder pattern for creating test data entities.
/// </summary>
public static class TestDataBuilders
{
    /// <summary>
    /// Creates a WeatherStation builder with default test values.
    /// </summary>
    public static WeatherStationBuilder WeatherStation() => new();

    /// <summary>
    /// Creates a StationData builder with default test values.
    /// </summary>
    public static StationDataBuilder StationData() => new();

    /// <summary>
    /// Creates a LatestStationData builder with default test values.
    /// </summary>
    public static LatestStationDataBuilder LatestStationData() => new();

    /// <summary>
    /// Creates an OpenMeteoDto builder with default test values.
    /// </summary>
    public static OpenMeteoDtoBuilder OpenMeteoDto() => new();

    /// <summary>
    /// Creates a MetYrDto builder with default test values.
    /// </summary>
    public static MetYrDtoBuilder MetYrDto() => new();

    /// <summary>
    /// Creates a ParaglidingLocation builder with default test values.
    /// </summary>
    public static ParaglidingLocationBuilder ParaglidingLocation() => new();

    /// <summary>
    /// Creates a ForecastCache builder with default test values.
    /// </summary>
    public static ForecastCacheBuilder ForecastCache() => new();
}

/// <summary>
/// Builder for creating WeatherStation test data.
/// </summary>
public class WeatherStationBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Station";
    private decimal _latitude = 60.12345678m;
    private decimal _longitude = 10.12345678m;
    private int? _altitude = 100;
    private string? _country = "Norway";
    private bool _isActive = true;
    private string? _provider = "MET";
    private DateTime? _updatedAt = DateTime.UtcNow;
    private string _stationId = "TEST-001";
    private bool _isMain = false;

    public WeatherStationBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public WeatherStationBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public WeatherStationBuilder WithCoordinates(decimal latitude, decimal longitude)
    {
        _latitude = latitude;
        _longitude = longitude;
        return this;
    }

    public WeatherStationBuilder WithAltitude(int? altitude)
    {
        _altitude = altitude;
        return this;
    }

    public WeatherStationBuilder WithCountry(string? country)
    {
        _country = country;
        return this;
    }

    public WeatherStationBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public WeatherStationBuilder WithProvider(string? provider)
    {
        _provider = provider;
        return this;
    }

    public WeatherStationBuilder WithUpdatedAt(DateTime? updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public WeatherStationBuilder WithStationId(string stationId)
    {
        _stationId = stationId;
        return this;
    }

    public WeatherStationBuilder WithIsMain(bool isMain)
    {
        _isMain = isMain;
        return this;
    }

    public WeatherStation Build()
    {
        return new WeatherStation
        {
            Id = _id,
            Name = _name,
            Latitude = _latitude,
            Longitude = _longitude,
            Altitude = _altitude,
            Country = _country,
            IsActive = _isActive,
            Provider = _provider,
            UpdatedAt = _updatedAt,
            StationId = _stationId,
            IsMain = _isMain
        };
    }
}

/// <summary>
/// Builder for creating StationData test data.
/// </summary>
public class StationDataBuilder
{
    private Guid _id = Guid.NewGuid();
    private decimal _windSpeed = 10.5m;
    private decimal? _windGust = 15.2m;
    private decimal? _windMinSpeed = 5.0m;
    private int _direction = 180;
    private decimal? _temperature = 20.5m;
    private DateTime _updatedAt = DateTime.UtcNow;
    private bool _isCompressed = false;
    private string _stationId = "TEST-001";

    public StationDataBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public StationDataBuilder WithWindSpeed(decimal windSpeed)
    {
        _windSpeed = windSpeed;
        return this;
    }

    public StationDataBuilder WithWindGust(decimal? windGust)
    {
        _windGust = windGust;
        return this;
    }

    public StationDataBuilder WithWindMinSpeed(decimal? windMinSpeed)
    {
        _windMinSpeed = windMinSpeed;
        return this;
    }

    public StationDataBuilder WithDirection(int direction)
    {
        _direction = direction;
        return this;
    }

    public StationDataBuilder WithTemperature(decimal? temperature)
    {
        _temperature = temperature;
        return this;
    }

    public StationDataBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public StationDataBuilder WithIsCompressed(bool isCompressed)
    {
        _isCompressed = isCompressed;
        return this;
    }

    public StationDataBuilder WithStationId(string stationId)
    {
        _stationId = stationId;
        return this;
    }

    public StationData Build()
    {
        return new StationData
        {
            Id = _id,
            WindSpeed = _windSpeed,
            WindGust = _windGust,
            WindMinSpeed = _windMinSpeed,
            Direction = _direction,
            Temperature = _temperature,
            UpdatedAt = _updatedAt,
            IsCompressed = _isCompressed,
            StationId = _stationId
        };
    }
}

/// <summary>
/// Builder for creating LatestStationData test data.
/// </summary>
public class LatestStationDataBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _stationId = "TEST-001";
    private decimal _windSpeed = 10.5m;
    private decimal? _windGust = 15.2m;
    private int _direction = 180;
    private decimal? _temperature = 20.5m;
    private DateTime _updatedAt = DateTime.UtcNow;
    private decimal? _windMinSpeed = 5.0m;

    public LatestStationDataBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public LatestStationDataBuilder WithStationId(string stationId)
    {
        _stationId = stationId;
        return this;
    }

    public LatestStationDataBuilder WithWindSpeed(decimal windSpeed)
    {
        _windSpeed = windSpeed;
        return this;
    }

    public LatestStationDataBuilder WithWindGust(decimal? windGust)
    {
        _windGust = windGust;
        return this;
    }

    public LatestStationDataBuilder WithDirection(int direction)
    {
        _direction = direction;
        return this;
    }

    public LatestStationDataBuilder WithTemperature(decimal? temperature)
    {
        _temperature = temperature;
        return this;
    }

    public LatestStationDataBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public LatestStationDataBuilder WithWindMinSpeed(decimal? windMinSpeed)
    {
        _windMinSpeed = windMinSpeed;
        return this;
    }

    public LatestStationData Build()
    {
        return new LatestStationData
        {
            Id = _id,
            StationId = _stationId,
            WindSpeed = _windSpeed,
            WindGust = _windGust,
            Direction = _direction,
            Temperature = _temperature,
            UpdatedAt = _updatedAt,
            WindMinSpeed = _windMinSpeed
        };
    }
}

/// <summary>
/// Builder for creating OpenMeteoDto test data.
/// </summary>
public class OpenMeteoDtoBuilder
{
    private string _time = "2024-01-01T12:00";
    private decimal _temperature2m = 15.5m;
    private decimal _windSpeed10m = 10.0m;
    private double _windDirection10m = 180.0;
    private decimal _windGusts10m = 15.0m;
    private decimal _precipitation = 0.0m;
    private float _precipitationProbability = 0.0f;
    private decimal _pressureMsl = 1013.25m;
    private string _weatherCode = "clear_sky";
    private int _isDay = 1;
    private decimal _windSpeed1000hPa = 12.0m;
    private double _windDirection1000hPa = 180.0;
    private decimal _windSpeed925hPa = 14.0m;
    private double _windDirection925hPa = 180.0;
    private decimal _windSpeed850hPa = 16.0m;
    private double _windDirection850hPa = 180.0;
    private decimal _windSpeed700hPa = 18.0m;
    private double _windDirection700hPa = 180.0;
    private decimal _temperature1000hPa = 10.0m;
    private decimal _temperature925hPa = 8.0m;
    private decimal _temperature850hPa = 5.0m;
    private decimal _temperature700hPa = 0.0m;
    private int _cloudCover = 0;
    private int _cloudCoverLow = 0;
    private int _cloudCoverMid = 0;
    private int _cloudCoverHigh = 0;
    private decimal _cape = 0.0m;
    private decimal _convectiveInhibition = 0.0m;
    private decimal _liftedIndex = 0.0m;
    private decimal _boundaryLayerHeight = 1000.0m;
    private decimal _freezingLevelHeight = 2000.0m;
    private decimal _geopotentialHeight1000hPa = 100.0m;
    private decimal _geopotentialHeight925hPa = 800.0m;
    private decimal _geopotentialHeight850hPa = 1500.0m;
    private decimal _geopotentialHeight700hPa = 3000.0m;

    public OpenMeteoDtoBuilder WithTime(string time)
    {
        _time = time;
        return this;
    }

    public OpenMeteoDtoBuilder WithTemperature2m(decimal temperature)
    {
        _temperature2m = temperature;
        return this;
    }

    public OpenMeteoDtoBuilder WithWindSpeed10m(decimal windSpeed)
    {
        _windSpeed10m = windSpeed;
        return this;
    }

    public OpenMeteoDtoBuilder WithWindDirection10m(double windDirection)
    {
        _windDirection10m = windDirection;
        return this;
    }

    public OpenMeteoDtoBuilder WithWindGusts10m(decimal windGusts)
    {
        _windGusts10m = windGusts;
        return this;
    }

    public OpenMeteoDtoBuilder WithPrecipitation(decimal precipitation)
    {
        _precipitation = precipitation;
        return this;
    }

    public OpenMeteoDtoBuilder WithPrecipitationProbability(float probability)
    {
        _precipitationProbability = probability;
        return this;
    }

    public OpenMeteoDtoBuilder WithPressureMsl(decimal pressure)
    {
        _pressureMsl = pressure;
        return this;
    }

    public OpenMeteoDtoBuilder WithWeatherCode(string weatherCode)
    {
        _weatherCode = weatherCode;
        return this;
    }

    public OpenMeteoDtoBuilder WithIsDay(int isDay)
    {
        _isDay = isDay;
        return this;
    }

    public OpenMeteoDtoBuilder WithWindSpeed1000hPa(decimal windSpeed)
    {
        _windSpeed1000hPa = windSpeed;
        return this;
    }

    public OpenMeteoDtoBuilder WithWindDirection1000hPa(double windDirection)
    {
        _windDirection1000hPa = windDirection;
        return this;
    }

    public OpenMeteoDtoBuilder WithWindSpeed925hPa(decimal windSpeed)
    {
        _windSpeed925hPa = windSpeed;
        return this;
    }

    public OpenMeteoDtoBuilder WithWindDirection925hPa(double windDirection)
    {
        _windDirection925hPa = windDirection;
        return this;
    }

    public OpenMeteoDtoBuilder WithWindSpeed850hPa(decimal windSpeed)
    {
        _windSpeed850hPa = windSpeed;
        return this;
    }

    public OpenMeteoDtoBuilder WithWindDirection850hPa(double windDirection)
    {
        _windDirection850hPa = windDirection;
        return this;
    }

    public OpenMeteoDtoBuilder WithWindSpeed700hPa(decimal windSpeed)
    {
        _windSpeed700hPa = windSpeed;
        return this;
    }

    public OpenMeteoDtoBuilder WithWindDirection700hPa(double windDirection)
    {
        _windDirection700hPa = windDirection;
        return this;
    }

    public OpenMeteoDtoBuilder WithTemperature1000hPa(decimal temperature)
    {
        _temperature1000hPa = temperature;
        return this;
    }

    public OpenMeteoDtoBuilder WithTemperature925hPa(decimal temperature)
    {
        _temperature925hPa = temperature;
        return this;
    }

    public OpenMeteoDtoBuilder WithTemperature850hPa(decimal temperature)
    {
        _temperature850hPa = temperature;
        return this;
    }

    public OpenMeteoDtoBuilder WithTemperature700hPa(decimal temperature)
    {
        _temperature700hPa = temperature;
        return this;
    }

    public OpenMeteoDtoBuilder WithCloudCover(int cloudCover)
    {
        _cloudCover = cloudCover;
        return this;
    }

    public OpenMeteoDtoBuilder WithCloudCoverLow(int cloudCover)
    {
        _cloudCoverLow = cloudCover;
        return this;
    }

    public OpenMeteoDtoBuilder WithCloudCoverMid(int cloudCover)
    {
        _cloudCoverMid = cloudCover;
        return this;
    }

    public OpenMeteoDtoBuilder WithCloudCoverHigh(int cloudCover)
    {
        _cloudCoverHigh = cloudCover;
        return this;
    }

    public OpenMeteoDtoBuilder WithCape(decimal cape)
    {
        _cape = cape;
        return this;
    }

    public OpenMeteoDtoBuilder WithConvectiveInhibition(decimal convectiveInhibition)
    {
        _convectiveInhibition = convectiveInhibition;
        return this;
    }

    public OpenMeteoDtoBuilder WithLiftedIndex(decimal liftedIndex)
    {
        _liftedIndex = liftedIndex;
        return this;
    }

    public OpenMeteoDtoBuilder WithBoundaryLayerHeight(decimal height)
    {
        _boundaryLayerHeight = height;
        return this;
    }

    public OpenMeteoDtoBuilder WithFreezingLevelHeight(decimal height)
    {
        _freezingLevelHeight = height;
        return this;
    }

    public OpenMeteoDtoBuilder WithGeopotentialHeight1000hPa(decimal height)
    {
        _geopotentialHeight1000hPa = height;
        return this;
    }

    public OpenMeteoDtoBuilder WithGeopotentialHeight925hPa(decimal height)
    {
        _geopotentialHeight925hPa = height;
        return this;
    }

    public OpenMeteoDtoBuilder WithGeopotentialHeight850hPa(decimal height)
    {
        _geopotentialHeight850hPa = height;
        return this;
    }

    public OpenMeteoDtoBuilder WithGeopotentialHeight700hPa(decimal height)
    {
        _geopotentialHeight700hPa = height;
        return this;
    }

    public OpenMeteoDto Build()
    {
        return new OpenMeteoDto
        {
            Time = _time,
            Temperature2m = _temperature2m,
            WindSpeed10m = _windSpeed10m,
            WindDirection10m = _windDirection10m,
            WindGusts10m = _windGusts10m,
            Precipitation = _precipitation,
            PrecipitationProbability = _precipitationProbability,
            PressureMsl = _pressureMsl,
            WeatherCode = _weatherCode,
            IsDay = _isDay,
            WindSpeed1000hPa = _windSpeed1000hPa,
            WindDirection1000hPa = _windDirection1000hPa,
            WindSpeed925hPa = _windSpeed925hPa,
            WindDirection925hPa = _windDirection925hPa,
            WindSpeed850hPa = _windSpeed850hPa,
            WindDirection850hPa = _windDirection850hPa,
            WindSpeed700hPa = _windSpeed700hPa,
            WindDirection700hPa = _windDirection700hPa,
            Temperature1000hPa = _temperature1000hPa,
            Temperature925hPa = _temperature925hPa,
            Temperature850hPa = _temperature850hPa,
            Temperature700hPa = _temperature700hPa,
            CloudCover = _cloudCover,
            CloudCoverLow = _cloudCoverLow,
            CloudCoverMid = _cloudCoverMid,
            CloudCoverHigh = _cloudCoverHigh,
            Cape = _cape,
            ConvectiveInhibition = _convectiveInhibition,
            LiftedIndex = _liftedIndex,
            BoundaryLayerHeight = _boundaryLayerHeight,
            FreezingLevelHeight = _freezingLevelHeight,
            GeopotentialHeight1000hPa = _geopotentialHeight1000hPa,
            GeopotentialHeight925hPa = _geopotentialHeight925hPa,
            GeopotentialHeight850hPa = _geopotentialHeight850hPa,
            GeopotentialHeight700hPa = _geopotentialHeight700hPa
        };
    }
}

/// <summary>
/// Builder for creating MetYrDto test data.
/// </summary>
public class MetYrDtoBuilder
{
    private string _time = "2024-01-01T12:00:00Z";
    private decimal _airPressureAtSeaLevel = 1013.25m;
    private decimal _airTemperature = 15.5m;
    private decimal? _airTemperaturePercentile10 = null;
    private decimal? _airTemperaturePercentile90 = null;
    private double _cloudAreaFraction = 0.0;
    private double _cloudAreaFractionHigh = 0.0;
    private double _cloudAreaFractionLow = 0.0;
    private double _cloudAreaFractionMedium = 0.0;
    private decimal _dewPointTemperature = 10.0m;
    private double _relativeHumidity = 60.0;
    private double _windFromDirection = 180.0;
    private decimal _windSpeed = 10.0m;
    private decimal _precipitationAmount = 0.0m;
    private double? _precipitationAmountMax = null;
    private double? _precipitationAmountMin = null;
    private float? _probabilityOfPrecipitation = null;
    private string _symbolCode = "clearsky_day";
    private double _fogAreaFraction = 0.0;
    private double _ultravioletIndexClearSky = 3.0;
    private decimal? _windSpeedOfGust = null;
    private double? _probabilityOfThunder = null;
    private string _next6HoursSymbolCode = "clearsky_day";

    public MetYrDtoBuilder WithTime(string time)
    {
        _time = time;
        return this;
    }

    public MetYrDtoBuilder WithAirPressureAtSeaLevel(decimal pressure)
    {
        _airPressureAtSeaLevel = pressure;
        return this;
    }

    public MetYrDtoBuilder WithAirTemperature(decimal temperature)
    {
        _airTemperature = temperature;
        return this;
    }

    public MetYrDtoBuilder WithAirTemperaturePercentile10(decimal? percentile)
    {
        _airTemperaturePercentile10 = percentile;
        return this;
    }

    public MetYrDtoBuilder WithAirTemperaturePercentile90(decimal? percentile)
    {
        _airTemperaturePercentile90 = percentile;
        return this;
    }

    public MetYrDtoBuilder WithCloudAreaFraction(double fraction)
    {
        _cloudAreaFraction = fraction;
        return this;
    }

    public MetYrDtoBuilder WithCloudAreaFractionHigh(double fraction)
    {
        _cloudAreaFractionHigh = fraction;
        return this;
    }

    public MetYrDtoBuilder WithCloudAreaFractionLow(double fraction)
    {
        _cloudAreaFractionLow = fraction;
        return this;
    }

    public MetYrDtoBuilder WithCloudAreaFractionMedium(double fraction)
    {
        _cloudAreaFractionMedium = fraction;
        return this;
    }

    public MetYrDtoBuilder WithDewPointTemperature(decimal temperature)
    {
        _dewPointTemperature = temperature;
        return this;
    }

    public MetYrDtoBuilder WithRelativeHumidity(double humidity)
    {
        _relativeHumidity = humidity;
        return this;
    }

    public MetYrDtoBuilder WithWindFromDirection(double direction)
    {
        _windFromDirection = direction;
        return this;
    }

    public MetYrDtoBuilder WithWindSpeed(decimal windSpeed)
    {
        _windSpeed = windSpeed;
        return this;
    }

    public MetYrDtoBuilder WithPrecipitationAmount(decimal amount)
    {
        _precipitationAmount = amount;
        return this;
    }

    public MetYrDtoBuilder WithPrecipitationAmountMax(double? max)
    {
        _precipitationAmountMax = max;
        return this;
    }

    public MetYrDtoBuilder WithPrecipitationAmountMin(double? min)
    {
        _precipitationAmountMin = min;
        return this;
    }

    public MetYrDtoBuilder WithProbabilityOfPrecipitation(float? probability)
    {
        _probabilityOfPrecipitation = probability;
        return this;
    }

    public MetYrDtoBuilder WithSymbolCode(string symbolCode)
    {
        _symbolCode = symbolCode;
        return this;
    }

    public MetYrDtoBuilder WithFogAreaFraction(double fraction)
    {
        _fogAreaFraction = fraction;
        return this;
    }

    public MetYrDtoBuilder WithUltravioletIndexClearSky(double index)
    {
        _ultravioletIndexClearSky = index;
        return this;
    }

    public MetYrDtoBuilder WithWindSpeedOfGust(decimal? gust)
    {
        _windSpeedOfGust = gust;
        return this;
    }

    public MetYrDtoBuilder WithProbabilityOfThunder(double? probability)
    {
        _probabilityOfThunder = probability;
        return this;
    }

    public MetYrDtoBuilder WithNext6HoursSymbolCode(string symbolCode)
    {
        _next6HoursSymbolCode = symbolCode;
        return this;
    }

    public MetYrDto Build()
    {
        return new MetYrDto
        {
            Time = _time,
            AirPressureAtSeaLevel = _airPressureAtSeaLevel,
            AirTemperature = _airTemperature,
            AirTemperaturePercentile10 = _airTemperaturePercentile10,
            AirTemperaturePercentile90 = _airTemperaturePercentile90,
            CloudAreaFraction = _cloudAreaFraction,
            CloudAreaFractionHigh = _cloudAreaFractionHigh,
            CloudAreaFractionLow = _cloudAreaFractionLow,
            CloudAreaFractionMedium = _cloudAreaFractionMedium,
            DewPointTemperature = _dewPointTemperature,
            RelativeHumidity = _relativeHumidity,
            WindFromDirection = _windFromDirection,
            WindSpeed = _windSpeed,
            PrecipitationAmount = _precipitationAmount,
            PrecipitationAmountMax = _precipitationAmountMax,
            PrecipitationAmountMin = _precipitationAmountMin,
            ProbabilityOfPrecipitation = _probabilityOfPrecipitation,
            SymbolCode = _symbolCode,
            FogAreaFraction = _fogAreaFraction,
            UltravioletIndexClearSky = _ultravioletIndexClearSky,
            WindSpeedOfGust = _windSpeedOfGust,
            ProbabilityOfThunder = _probabilityOfThunder,
            Next6HoursSymbolCode = _next6HoursSymbolCode
        };
    }
}

/// <summary>
/// Builder for creating ParaglidingLocation test data.
/// </summary>
public class ParaglidingLocationBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Paragliding Location";
    private string? _description = "A test paragliding location";
    private float _longitude = 10.12345f;
    private float _latitude = 60.12345f;
    private int _altitude = 500;
    private string _country = "Norway";
    private string _flightlogId = "TEST-FL-001";
    private bool _isActive = true;
    private DateTime? _createdAt = DateTime.UtcNow;
    private DateTime? _updatedAt = DateTime.UtcNow;
    private bool _n = false;
    private bool _ne = false;
    private bool _e = false;
    private bool _se = false;
    private bool _s = true; // Default to south-facing
    private bool _sw = false;
    private bool _w = false;
    private bool _nw = false;
    private bool _isMain = false;
    private float? _landingLatitude = 60.11111f;
    private float? _landingLongitude = 10.11111f;
    private int? _landingAltitude = 100;
    private string? _timezone = "Europe/Oslo";

    public ParaglidingLocationBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public ParaglidingLocationBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ParaglidingLocationBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public ParaglidingLocationBuilder WithCoordinates(float latitude, float longitude)
    {
        _latitude = latitude;
        _longitude = longitude;
        return this;
    }

    public ParaglidingLocationBuilder WithAltitude(int altitude)
    {
        _altitude = altitude;
        return this;
    }

    public ParaglidingLocationBuilder WithCountry(string country)
    {
        _country = country;
        return this;
    }

    public ParaglidingLocationBuilder WithFlightlogId(string flightlogId)
    {
        _flightlogId = flightlogId;
        return this;
    }

    public ParaglidingLocationBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public ParaglidingLocationBuilder WithDirections(bool n = false, bool ne = false, bool e = false, bool se = false,
        bool s = false, bool sw = false, bool w = false, bool nw = false)
    {
        _n = n;
        _ne = ne;
        _e = e;
        _se = se;
        _s = s;
        _sw = sw;
        _w = w;
        _nw = nw;
        return this;
    }

    public ParaglidingLocationBuilder WithIsMain(bool isMain)
    {
        _isMain = isMain;
        return this;
    }

    public ParaglidingLocationBuilder WithLandingCoordinates(float? latitude, float? longitude, int? altitude = null)
    {
        _landingLatitude = latitude;
        _landingLongitude = longitude;
        _landingAltitude = altitude;
        return this;
    }

    public ParaglidingLocationBuilder WithTimezone(string? timezone)
    {
        _timezone = timezone;
        return this;
    }

    public ParaglidingLocation Build()
    {
        return new ParaglidingLocation
        {
            Id = _id,
            Name = _name,
            Description = _description,
            Longitude = _longitude,
            Latitude = _latitude,
            Altitude = _altitude,
            Country = _country,
            FlightlogId = _flightlogId,
            IsActive = _isActive,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt,
            N = _n,
            NE = _ne,
            E = _e,
            SE = _se,
            S = _s,
            SW = _sw,
            W = _w,
            NW = _nw,
            IsMain = _isMain,
            LandingLatitude = _landingLatitude,
            LandingLongitude = _landingLongitude,
            LandingAltitude = _landingAltitude,
            Timezone = _timezone
        };
    }
}

/// <summary>
/// Builder for creating ForecastCache test data.
/// </summary>
public class ForecastCacheBuilder
{
    private long _id = 0;
    private Guid _locationId = Guid.NewGuid();
    private DateTime _time = DateTime.UtcNow.AddHours(1);
    private decimal? _temperature = 15.5m;
    private decimal? _windSpeed = 10.0m;
    private int? _windDirection = 180;
    private decimal? _windGusts = 15.0m;
    private decimal? _precipitation = 0.0m;
    private float? _precipitationProbability = 10.0f;
    private decimal? _pressureMsl = 1013.25m;
    private string? _weatherCode = "clearsky_day";
    private short? _isDay = 1;
    private decimal? _landingWind = 8.0m;
    private decimal? _landingGust = 12.0m;
    private int? _landingWindDirection = 180;
    private decimal? _windSpeed1000hpa = 12.0m;
    private int? _windDirection1000hpa = 180;
    private decimal? _windSpeed925hpa = 14.0m;
    private int? _windDirection925hpa = 180;
    private decimal? _windSpeed850hpa = 16.0m;
    private int? _windDirection850hpa = 180;
    private decimal? _windSpeed700hpa = 18.0m;
    private int? _windDirection700hpa = 180;
    private decimal? _temperature1000hpa = 10.0m;
    private decimal? _temperature925hpa = 8.0m;
    private decimal? _temperature850hpa = 5.0m;
    private decimal? _temperature700hpa = 0.0m;
    private int? _cloudCover = 20;
    private int? _cloudCoverLow = 10;
    private int? _cloudCoverMid = 5;
    private int? _cloudCoverHigh = 5;
    private decimal? _cape = 0.0m;
    private decimal? _convectiveInhibition = 0.0m;
    private decimal? _liftedIndex = 0.0m;
    private decimal? _boundaryLayerHeight = 1000.0m;
    private decimal? _freezingLevelHeight = 2000.0m;
    private decimal? _geopotentialHeight1000hpa = 100.0m;
    private decimal? _geopotentialHeight925hpa = 800.0m;
    private decimal? _geopotentialHeight850hpa = 1500.0m;
    private decimal? _geopotentialHeight700hpa = 3000.0m;
    private DateTime? _createdAt = DateTime.UtcNow;
    private DateTime? _updatedAt = DateTime.UtcNow;
    private double? _precipitationMax = 0.0;
    private double? _precipitationMin = 0.0;
    private bool _isYrData = false;

    public ForecastCacheBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public ForecastCacheBuilder WithLocationId(Guid locationId)
    {
        _locationId = locationId;
        return this;
    }

    public ForecastCacheBuilder WithTime(DateTime time)
    {
        _time = time;
        return this;
    }

    public ForecastCacheBuilder WithTemperature(decimal? temperature)
    {
        _temperature = temperature;
        return this;
    }

    public ForecastCacheBuilder WithWindSpeed(decimal? windSpeed)
    {
        _windSpeed = windSpeed;
        return this;
    }

    public ForecastCacheBuilder WithWindDirection(int? windDirection)
    {
        _windDirection = windDirection;
        return this;
    }

    public ForecastCacheBuilder WithWindGusts(decimal? windGusts)
    {
        _windGusts = windGusts;
        return this;
    }

    public ForecastCacheBuilder WithPrecipitation(decimal? precipitation)
    {
        _precipitation = precipitation;
        return this;
    }

    public ForecastCacheBuilder WithPrecipitationProbability(float? probability)
    {
        _precipitationProbability = probability;
        return this;
    }

    public ForecastCacheBuilder WithPressureMsl(decimal? pressure)
    {
        _pressureMsl = pressure;
        return this;
    }

    public ForecastCacheBuilder WithWeatherCode(string? weatherCode)
    {
        _weatherCode = weatherCode;
        return this;
    }

    public ForecastCacheBuilder WithIsDay(short? isDay)
    {
        _isDay = isDay;
        return this;
    }

    public ForecastCacheBuilder WithLandingWind(decimal? landingWind)
    {
        _landingWind = landingWind;
        return this;
    }

    public ForecastCacheBuilder WithLandingGust(decimal? landingGust)
    {
        _landingGust = landingGust;
        return this;
    }

    public ForecastCacheBuilder WithLandingWindDirection(int? landingWindDirection)
    {
        _landingWindDirection = landingWindDirection;
        return this;
    }

    public ForecastCacheBuilder WithCloudCover(int? cloudCover)
    {
        _cloudCover = cloudCover;
        return this;
    }

    public ForecastCacheBuilder WithIsYrData(bool isYrData)
    {
        _isYrData = isYrData;
        return this;
    }

    public ForecastCacheBuilder WithUpdatedAt(DateTime? updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public ForecastCache Build()
    {
        return new ForecastCache
        {
            Id = _id,
            LocationId = _locationId,
            Time = _time,
            Temperature = _temperature,
            WindSpeed = _windSpeed,
            WindDirection = _windDirection,
            WindGusts = _windGusts,
            Precipitation = _precipitation,
            PrecipitationProbability = _precipitationProbability,
            PressureMsl = _pressureMsl,
            WeatherCode = _weatherCode,
            IsDay = _isDay,
            LandingWind = _landingWind,
            LandingGust = _landingGust,
            LandingWindDirection = _landingWindDirection,
            WindSpeed1000hpa = _windSpeed1000hpa,
            WindDirection1000hpa = _windDirection1000hpa,
            WindSpeed925hpa = _windSpeed925hpa,
            WindDirection925hpa = _windDirection925hpa,
            WindSpeed850hpa = _windSpeed850hpa,
            WindDirection850hpa = _windDirection850hpa,
            WindSpeed700hpa = _windSpeed700hpa,
            WindDirection700hpa = _windDirection700hpa,
            Temperature1000hpa = _temperature1000hpa,
            Temperature925hpa = _temperature925hpa,
            Temperature850hpa = _temperature850hpa,
            Temperature700hpa = _temperature700hpa,
            CloudCover = _cloudCover,
            CloudCoverLow = _cloudCoverLow,
            CloudCoverMid = _cloudCoverMid,
            CloudCoverHigh = _cloudCoverHigh,
            Cape = _cape,
            ConvectiveInhibition = _convectiveInhibition,
            LiftedIndex = _liftedIndex,
            BoundaryLayerHeight = _boundaryLayerHeight,
            FreezingLevelHeight = _freezingLevelHeight,
            GeopotentialHeight1000hpa = _geopotentialHeight1000hpa,
            GeopotentialHeight925hpa = _geopotentialHeight925hpa,
            GeopotentialHeight850hpa = _geopotentialHeight850hpa,
            GeopotentialHeight700hpa = _geopotentialHeight700hpa,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt,
            PrecipitationMax = _precipitationMax,
            PrecipitationMin = _precipitationMin,
            IsYrData = _isYrData
        };
    }
}


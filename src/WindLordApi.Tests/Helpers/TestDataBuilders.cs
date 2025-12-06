using WindLordApi.Data.Models;

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


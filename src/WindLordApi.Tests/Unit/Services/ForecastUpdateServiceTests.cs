using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Data.Models;
using WindLordApi.Data.Services;
using WindLordApi.Integrations.MetYr;
using WindLordApi.Integrations.OpenMeteo;
using WindLordApi.Tests.Helpers;
using WindLordApi.Worker.Services;
using System.Globalization;

namespace WindLordApi.Tests.Unit.Services;

/// <summary>
/// Unit tests for ForecastUpdateService with MetYr-only data flow.
/// </summary>
public class ForecastUpdateServiceTests
{
    private readonly Mock<IMetYrClient> _metYrClientMock;
    private readonly Mock<IMetYrMapping> _metYrMappingMock;
    private readonly Mock<IOpenMeteoClient> _openMeteoClientMock;
    private readonly Mock<IOpenMeteoMapping> _openMeteoMappingMock;
    private readonly Mock<IParaglidingLocationService> _paraglidingLocationServiceMock;
    private readonly Mock<IForecastCacheService> _forecastCacheServiceMock;
    private readonly Mock<ILogger<ForecastUpdateService>> _loggerMock;
    private readonly ForecastUpdateService _service;

    public ForecastUpdateServiceTests()
    {
        _metYrClientMock = new Mock<IMetYrClient>();
        _metYrMappingMock = new Mock<IMetYrMapping>();
        _openMeteoClientMock = new Mock<IOpenMeteoClient>();
        _openMeteoMappingMock = new Mock<IOpenMeteoMapping>();
        _paraglidingLocationServiceMock = new Mock<IParaglidingLocationService>();
        _forecastCacheServiceMock = new Mock<IForecastCacheService>();
        _loggerMock = new Mock<ILogger<ForecastUpdateService>>();

        _openMeteoClientMock
            .Setup(x => x.FetchForecastAsync(It.IsAny<IReadOnlyList<OpenMeteoRequestLocation>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<OpenMeteoRequestLocation> locations, DateTime _, DateTime _, CancellationToken _) =>
                locations.Select(location => new OpenMeteoForecastResponse
                {
                    Latitude = OpenMeteoCoordinates.TruncateToRequestPrecision(location.Latitude),
                    Longitude = OpenMeteoCoordinates.TruncateToRequestPrecision(location.Longitude),
                    Hourly = new OpenMeteoHourlyForecast()
                }).ToArray());

        _openMeteoMappingMock
            .Setup(x => x.MapForecasts(It.IsAny<IReadOnlyList<OpenMeteoForecastResponse>>()))
            .Returns((IReadOnlyList<OpenMeteoForecastResponse> responses) =>
                responses.Select(response => new OpenMeteoLocationForecast
                {
                    Latitude = response.Latitude,
                    Longitude = response.Longitude,
                    Forecasts = Array.Empty<OpenMeteoForecastPoint>()
                }).ToArray());

        _service = new ForecastUpdateService(
            _metYrClientMock.Object,
            _metYrMappingMock.Object,
            _openMeteoClientMock.Object,
            _openMeteoMappingMock.Object,
            _paraglidingLocationServiceMock.Object,
            _forecastCacheServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task UpdateForecastsAsync_WithValidLocation_ShouldFetchAndStoreMetYrData()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var location = TestDataBuilders.ParaglidingLocation()
            .WithId(locationId)
            .WithCoordinates(60.1234f, 10.5678f)
            .Build();

        var yrDto = TestDataBuilders.MetYrDto()
            .WithTime("2024-01-01T12:00:00Z")
            .WithAirTemperature(15.5m)
            .WithWindSpeed(10.0m)
            .WithWindFromDirection(180.0)
            .WithSymbolCode("clearsky_day")
            .Build();

        var yrModel = new WeatherDataYr
        {
            MetYrDto = new[] { yrDto },
            UpdatedAt = "2024-01-01T12:00:00Z",
            Elevation = 100.0,
            Location = new LocationInfo { Latitude = 60.1234, Longitude = 10.5678 }
        };

        _paraglidingLocationServiceMock
            .Setup(x => x.GetLocationsWithoutForecastAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new LocationsWithoutForecast { LocationId = locationId } });

        _paraglidingLocationServiceMock
            .Setup(x => x.GetByIdsAsync(It.Is<List<Guid>>(ids => ids.Contains(locationId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { location });

        _metYrClientMock
            .Setup(x => x.FetchYrDataAsync(location.Latitude, location.Longitude, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<MetYrResponse>());

        _metYrMappingMock
            .Setup(x => x.MapYrData(It.IsAny<MetYrResponse>()))
            .Returns(yrModel);

        _forecastCacheServiceMock
            .Setup(x => x.DeleteOldForecastsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _forecastCacheServiceMock
            .Setup(x => x.UpsertManyAsync(It.IsAny<ForecastCache[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.UpdateForecastsAsync(TestContext.Current.CancellationToken);

        // Assert
        _metYrClientMock.Verify(x => x.FetchYrDataAsync(location.Latitude, location.Longitude, It.IsAny<CancellationToken>()), Times.Once);
        _forecastCacheServiceMock.Verify(x => x.UpsertManyAsync(
            It.Is<ForecastCache[]>(fc => fc.Length == 1 && fc[0].LocationId == locationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateForecastsAsync_ShouldSetLocationId()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var location = TestDataBuilders.ParaglidingLocation()
            .WithId(locationId)
            .WithCoordinates(60.1234f, 10.5678f)
            .Build();

        var yrDto = TestDataBuilders.MetYrDto()
            .WithTime("2024-01-01T12:00:00Z")
            .WithSymbolCode("clearsky_day")
            .Build();

        var yrModel = new WeatherDataYr
        {
            MetYrDto = new[] { yrDto },
            UpdatedAt = "2024-01-01T12:00:00Z",
            Elevation = 100.0,
            Location = new LocationInfo { Latitude = 60.1234, Longitude = 10.5678 }
        };

        _paraglidingLocationServiceMock
            .Setup(x => x.GetLocationsWithoutForecastAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new LocationsWithoutForecast { LocationId = locationId } });

        _paraglidingLocationServiceMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { location });

        _metYrClientMock
            .Setup(x => x.FetchYrDataAsync(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<MetYrResponse>());

        _metYrMappingMock
            .Setup(x => x.MapYrData(It.IsAny<MetYrResponse>()))
            .Returns(yrModel);

        _forecastCacheServiceMock
            .Setup(x => x.DeleteOldForecastsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        ForecastCache[]? capturedForecastCache = null;
        _forecastCacheServiceMock
            .Setup(x => x.UpsertManyAsync(It.IsAny<ForecastCache[]>(), It.IsAny<CancellationToken>()))
            .Callback<ForecastCache[], CancellationToken>((fc, _) => capturedForecastCache = fc)
            .ReturnsAsync(1);

        // Act
        await _service.UpdateForecastsAsync(TestContext.Current.CancellationToken);

        // Assert
        capturedForecastCache.Should().NotBeNull();
        capturedForecastCache.Should().HaveCount(1);
        capturedForecastCache![0].LocationId.Should().Be(locationId);
    }

    [Fact]
    public async Task UpdateForecastsAsync_ShouldSetIsYrDataToTrue()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var location = TestDataBuilders.ParaglidingLocation()
            .WithId(locationId)
            .WithCoordinates(60.1234f, 10.5678f)
            .Build();

        var yrDto = TestDataBuilders.MetYrDto()
            .WithTime("2024-01-01T12:00:00Z")
            .WithSymbolCode("clearsky_day")
            .Build();

        var yrModel = new WeatherDataYr
        {
            MetYrDto = new[] { yrDto },
            UpdatedAt = "2024-01-01T12:00:00Z",
            Elevation = 100.0,
            Location = new LocationInfo { Latitude = 60.1234, Longitude = 10.5678 }
        };

        _paraglidingLocationServiceMock
            .Setup(x => x.GetLocationsWithoutForecastAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new LocationsWithoutForecast { LocationId = locationId } });

        _paraglidingLocationServiceMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { location });

        _metYrClientMock
            .Setup(x => x.FetchYrDataAsync(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<MetYrResponse>());

        _metYrMappingMock
            .Setup(x => x.MapYrData(It.IsAny<MetYrResponse>()))
            .Returns(yrModel);

        _forecastCacheServiceMock
            .Setup(x => x.DeleteOldForecastsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        ForecastCache[]? capturedForecastCache = null;
        _forecastCacheServiceMock
            .Setup(x => x.UpsertManyAsync(It.IsAny<ForecastCache[]>(), It.IsAny<CancellationToken>()))
            .Callback<ForecastCache[], CancellationToken>((fc, _) => capturedForecastCache = fc)
            .ReturnsAsync(1);

        // Act
        await _service.UpdateForecastsAsync(TestContext.Current.CancellationToken);

        // Assert
        capturedForecastCache.Should().NotBeNull();
        capturedForecastCache.Should().HaveCount(1);
        capturedForecastCache![0].IsYrData.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateForecastsAsync_ShouldSetAtmosphericFieldsToNull()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var location = TestDataBuilders.ParaglidingLocation()
            .WithId(locationId)
            .WithCoordinates(60.1234f, 10.5678f)
            .Build();

        var yrDto = TestDataBuilders.MetYrDto()
            .WithTime("2024-01-01T12:00:00Z")
            .WithSymbolCode("clearsky_day")
            .Build();

        var yrModel = new WeatherDataYr
        {
            MetYrDto = new[] { yrDto },
            UpdatedAt = "2024-01-01T12:00:00Z",
            Elevation = 100.0,
            Location = new LocationInfo { Latitude = 60.1234, Longitude = 10.5678 }
        };

        _paraglidingLocationServiceMock
            .Setup(x => x.GetLocationsWithoutForecastAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new LocationsWithoutForecast { LocationId = locationId } });

        _paraglidingLocationServiceMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { location });

        _metYrClientMock
            .Setup(x => x.FetchYrDataAsync(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<MetYrResponse>());

        _metYrMappingMock
            .Setup(x => x.MapYrData(It.IsAny<MetYrResponse>()))
            .Returns(yrModel);

        _forecastCacheServiceMock
            .Setup(x => x.DeleteOldForecastsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        ForecastCache[]? capturedForecastCache = null;
        _forecastCacheServiceMock
            .Setup(x => x.UpsertManyAsync(It.IsAny<ForecastCache[]>(), It.IsAny<CancellationToken>()))
            .Callback<ForecastCache[], CancellationToken>((fc, _) => capturedForecastCache = fc)
            .ReturnsAsync(1);

        // Act
        await _service.UpdateForecastsAsync(TestContext.Current.CancellationToken);

        // Assert
        capturedForecastCache.Should().NotBeNull();
        var forecast = capturedForecastCache![0];

        // Verify atmospheric fields are null
        forecast.WindSpeed1000hpa.Should().BeNull();
        forecast.WindDirection1000hpa.Should().BeNull();
        forecast.WindSpeed925hpa.Should().BeNull();
        forecast.WindDirection925hpa.Should().BeNull();
        forecast.WindSpeed850hpa.Should().BeNull();
        forecast.WindDirection850hpa.Should().BeNull();
        forecast.WindSpeed700hpa.Should().BeNull();
        forecast.WindDirection700hpa.Should().BeNull();
        forecast.Temperature1000hpa.Should().BeNull();
        forecast.Temperature925hpa.Should().BeNull();
        forecast.Temperature850hpa.Should().BeNull();
        forecast.Temperature700hpa.Should().BeNull();
        forecast.CloudCover.Should().BeNull();
        forecast.CloudCoverLow.Should().BeNull();
        forecast.CloudCoverMid.Should().BeNull();
        forecast.CloudCoverHigh.Should().BeNull();
        forecast.Cape.Should().BeNull();
        forecast.ConvectiveInhibition.Should().BeNull();
        forecast.LiftedIndex.Should().BeNull();
        forecast.BoundaryLayerHeight.Should().BeNull();
        forecast.FreezingLevelHeight.Should().BeNull();
        forecast.GeopotentialHeight1000hpa.Should().BeNull();
        forecast.GeopotentialHeight925hpa.Should().BeNull();
        forecast.GeopotentialHeight850hpa.Should().BeNull();
        forecast.GeopotentialHeight700hpa.Should().BeNull();
    }

    [Fact]
    public async Task UpdateForecastsAsync_WithNightSymbolCode_ShouldSetIsDayToZero()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var location = TestDataBuilders.ParaglidingLocation()
            .WithId(locationId)
            .WithCoordinates(60.1234f, 10.5678f)
            .Build();

        var yrDto = TestDataBuilders.MetYrDto()
            .WithTime("2024-01-01T22:00:00Z")
            .WithSymbolCode("clearsky_night")
            .Build();

        var yrModel = new WeatherDataYr
        {
            MetYrDto = new[] { yrDto },
            UpdatedAt = "2024-01-01T12:00:00Z",
            Elevation = 100.0,
            Location = new LocationInfo { Latitude = 60.1234, Longitude = 10.5678 }
        };

        _paraglidingLocationServiceMock
            .Setup(x => x.GetLocationsWithoutForecastAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new LocationsWithoutForecast { LocationId = locationId } });

        _paraglidingLocationServiceMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { location });

        _metYrClientMock
            .Setup(x => x.FetchYrDataAsync(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<MetYrResponse>());

        _metYrMappingMock
            .Setup(x => x.MapYrData(It.IsAny<MetYrResponse>()))
            .Returns(yrModel);

        _forecastCacheServiceMock
            .Setup(x => x.DeleteOldForecastsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        ForecastCache[]? capturedForecastCache = null;
        _forecastCacheServiceMock
            .Setup(x => x.UpsertManyAsync(It.IsAny<ForecastCache[]>(), It.IsAny<CancellationToken>()))
            .Callback<ForecastCache[], CancellationToken>((fc, _) => capturedForecastCache = fc)
            .ReturnsAsync(1);

        // Act
        await _service.UpdateForecastsAsync(TestContext.Current.CancellationToken);

        // Assert
        capturedForecastCache.Should().NotBeNull();
        capturedForecastCache![0].IsDay.Should().Be(0);
    }

    [Fact]
    public async Task UpdateForecastsAsync_WithDaySymbolCode_ShouldSetIsDayToOne()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var location = TestDataBuilders.ParaglidingLocation()
            .WithId(locationId)
            .WithCoordinates(60.1234f, 10.5678f)
            .Build();

        var yrDto = TestDataBuilders.MetYrDto()
            .WithTime("2024-01-01T12:00:00Z")
            .WithSymbolCode("clearsky_day")
            .Build();

        var yrModel = new WeatherDataYr
        {
            MetYrDto = new[] { yrDto },
            UpdatedAt = "2024-01-01T12:00:00Z",
            Elevation = 100.0,
            Location = new LocationInfo { Latitude = 60.1234, Longitude = 10.5678 }
        };

        _paraglidingLocationServiceMock
            .Setup(x => x.GetLocationsWithoutForecastAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new LocationsWithoutForecast { LocationId = locationId } });

        _paraglidingLocationServiceMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { location });

        _metYrClientMock
            .Setup(x => x.FetchYrDataAsync(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<MetYrResponse>());

        _metYrMappingMock
            .Setup(x => x.MapYrData(It.IsAny<MetYrResponse>()))
            .Returns(yrModel);

        _forecastCacheServiceMock
            .Setup(x => x.DeleteOldForecastsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        ForecastCache[]? capturedForecastCache = null;
        _forecastCacheServiceMock
            .Setup(x => x.UpsertManyAsync(It.IsAny<ForecastCache[]>(), It.IsAny<CancellationToken>()))
            .Callback<ForecastCache[], CancellationToken>((fc, _) => capturedForecastCache = fc)
            .ReturnsAsync(1);

        // Act
        await _service.UpdateForecastsAsync(TestContext.Current.CancellationToken);

        // Assert
        capturedForecastCache.Should().NotBeNull();
        capturedForecastCache![0].IsDay.Should().Be(1);
    }

    [Fact]
    public async Task UpdateForecastsAsync_WithLandingCoordinates_ShouldMergeLandingData()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var location = TestDataBuilders.ParaglidingLocation()
            .WithId(locationId)
            .WithCoordinates(60.1234f, 10.5678f)
            .WithLandingCoordinates(60.1111f, 10.5555f)
            .Build();

        var yrTakeoffDto = TestDataBuilders.MetYrDto()
            .WithTime("2024-01-01T12:00:00Z")
            .WithWindSpeed(10.0m)
            .WithSymbolCode("clearsky_day")
            .Build();

        var yrLandingDto = TestDataBuilders.MetYrDto()
            .WithTime("2024-01-01T12:00:00Z")
            .WithWindSpeed(5.0m)
            .WithWindSpeedOfGust(8.0m)
            .WithWindFromDirection(270.0)
            .WithSymbolCode("clearsky_day")
            .Build();

        var yrTakeoffModel = new WeatherDataYr
        {
            MetYrDto = new[] { yrTakeoffDto },
            UpdatedAt = "2024-01-01T12:00:00Z",
            Elevation = 100.0,
            Location = new LocationInfo { Latitude = 60.1234, Longitude = 10.5678 }
        };
        var yrLandingModel = new WeatherDataYr
        {
            MetYrDto = new[] { yrLandingDto },
            UpdatedAt = "2024-01-01T12:00:00Z",
            Elevation = 100.0,
            Location = new LocationInfo { Latitude = 60.1111, Longitude = 10.5555 }
        };

        _paraglidingLocationServiceMock
            .Setup(x => x.GetLocationsWithoutForecastAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new LocationsWithoutForecast { LocationId = locationId } });

        _paraglidingLocationServiceMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { location });

        _metYrClientMock
            .Setup(x => x.FetchYrDataAsync(location.Latitude, location.Longitude, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<MetYrResponse>());

        _metYrClientMock
            .Setup(x => x.FetchYrDataAsync(location.LandingLatitude!.Value, location.LandingLongitude!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<MetYrResponse>());

        _metYrMappingMock
            .SetupSequence(x => x.MapYrData(It.IsAny<MetYrResponse>()))
            .Returns(yrTakeoffModel)
            .Returns(yrLandingModel);

        _forecastCacheServiceMock
            .Setup(x => x.DeleteOldForecastsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        ForecastCache[]? capturedForecastCache = null;
        _forecastCacheServiceMock
            .Setup(x => x.UpsertManyAsync(It.IsAny<ForecastCache[]>(), It.IsAny<CancellationToken>()))
            .Callback<ForecastCache[], CancellationToken>((fc, _) => capturedForecastCache = fc)
            .ReturnsAsync(1);

        // Act
        await _service.UpdateForecastsAsync(TestContext.Current.CancellationToken);

        // Assert
        capturedForecastCache.Should().NotBeNull();
        var forecast = capturedForecastCache![0];
        forecast.LandingWind.Should().Be(5.0m);
        forecast.LandingGust.Should().Be(8.0m);
        forecast.LandingWindDirection.Should().Be(270);
    }

    [Fact]
    public async Task UpdateForecastsAsync_WhenLocationFails_ShouldContinueProcessingOthers()
    {
        // Arrange
        var locationId1 = Guid.NewGuid();
        var locationId2 = Guid.NewGuid();

        var location1 = TestDataBuilders.ParaglidingLocation()
            .WithId(locationId1)
            .WithCoordinates(60.1234f, 10.5678f)
            .Build();

        var location2 = TestDataBuilders.ParaglidingLocation()
            .WithId(locationId2)
            .WithCoordinates(61.1234f, 11.5678f)
            .Build();

        var yrDto = TestDataBuilders.MetYrDto()
            .WithTime("2024-01-01T12:00:00Z")
            .WithSymbolCode("clearsky_day")
            .Build();

        var yrModel = new WeatherDataYr
        {
            MetYrDto = new[] { yrDto },
            UpdatedAt = "2024-01-01T12:00:00Z",
            Elevation = 100.0,
            Location = new LocationInfo { Latitude = 60.1234, Longitude = 10.5678 }
        };

        _paraglidingLocationServiceMock
            .Setup(x => x.GetLocationsWithoutForecastAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new LocationsWithoutForecast { LocationId = locationId1 }, new LocationsWithoutForecast { LocationId = locationId2 } });

        _paraglidingLocationServiceMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { location1, location2 });

        // First location throws exception
        _metYrClientMock
            .Setup(x => x.FetchYrDataAsync(location1.Latitude, location1.Longitude, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API error"));

        // Second location succeeds
        _metYrClientMock
            .Setup(x => x.FetchYrDataAsync(location2.Latitude, location2.Longitude, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<MetYrResponse>());

        _metYrMappingMock
            .Setup(x => x.MapYrData(It.IsAny<MetYrResponse>()))
            .Returns(yrModel);

        _forecastCacheServiceMock
            .Setup(x => x.DeleteOldForecastsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _forecastCacheServiceMock
            .Setup(x => x.UpsertManyAsync(It.IsAny<ForecastCache[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.UpdateForecastsAsync(TestContext.Current.CancellationToken);

        // Assert - Second location should still be processed
        _forecastCacheServiceMock.Verify(x => x.UpsertManyAsync(
            It.Is<ForecastCache[]>(fc => fc[0].LocationId == locationId2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateForecastsAsync_WhenOpenMeteoReturnsLaterRows_ShouldAppendSupplementalRows()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var location = TestDataBuilders.ParaglidingLocation()
            .WithId(locationId)
            .WithCoordinates(60.1234f, 10.5678f)
            .Build();

        var yrDto = TestDataBuilders.MetYrDto()
            .WithTime("2024-01-01T12:00:00Z")
            .WithAirTemperature(12.3m)
            .WithSymbolCode("clearsky_day")
            .Build();

        var yrModel = new WeatherDataYr
        {
            MetYrDto = new[] { yrDto },
            UpdatedAt = "2024-01-01T12:00:00Z",
            Elevation = 100.0,
            Location = new LocationInfo { Latitude = 60.1234, Longitude = 10.5678 }
        };

        _paraglidingLocationServiceMock
            .Setup(x => x.GetLocationsWithoutForecastAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new LocationsWithoutForecast { LocationId = locationId } });

        _paraglidingLocationServiceMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { location });

        _metYrClientMock
            .Setup(x => x.FetchYrDataAsync(location.Latitude, location.Longitude, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<MetYrResponse>());

        _metYrMappingMock
            .Setup(x => x.MapYrData(It.IsAny<MetYrResponse>()))
            .Returns(yrModel);

        _openMeteoMappingMock
            .Setup(x => x.MapForecasts(It.IsAny<IReadOnlyList<OpenMeteoForecastResponse>>()))
            .Returns(new[]
            {
                new OpenMeteoLocationForecast
                {
                    Latitude = OpenMeteoCoordinates.TruncateToRequestPrecision(location.Latitude),
                    Longitude = OpenMeteoCoordinates.TruncateToRequestPrecision(location.Longitude),
                    Forecasts = new[]
                    {
                        new OpenMeteoForecastPoint
                        {
                            Time = DateTime.Parse("2024-01-01T13:00:00Z", null, DateTimeStyles.AdjustToUniversal),
                            Temperature = 10.1m,
                            WindSpeed = 6.2m,
                            WindDirection = 190,
                            WindGusts = 7.3m,
                            Precipitation = 0.25m,
                            PrecipitationProbability = 25f,
                            PressureMsl = 1010.1m,
                            WeatherCode = "cloudy",
                            IsDay = 1
                        }
                    }
                }
            });

        _forecastCacheServiceMock
            .Setup(x => x.DeleteOldForecastsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        ForecastCache[]? capturedForecastCache = null;
        _forecastCacheServiceMock
            .Setup(x => x.UpsertManyAsync(It.IsAny<ForecastCache[]>(), It.IsAny<CancellationToken>()))
            .Callback<ForecastCache[], CancellationToken>((fc, _) => capturedForecastCache = fc)
            .ReturnsAsync(2);

        // Act
        await _service.UpdateForecastsAsync(TestContext.Current.CancellationToken);

        // Assert
        capturedForecastCache.Should().NotBeNull();
        capturedForecastCache.Should().HaveCount(2);
        capturedForecastCache![0].IsYrData.Should().BeTrue();
        capturedForecastCache[1].IsYrData.Should().BeFalse();
        capturedForecastCache[1].Time.Should().Be(DateTime.Parse("2024-01-01T13:00:00Z").ToUniversalTime());
        capturedForecastCache[1].WindGusts.Should().BeNull();
        capturedForecastCache[1].PrecipitationMax.Should().BeNull();
        capturedForecastCache[1].PrecipitationMin.Should().BeNull();
    }

    [Fact]
    public async Task UpdateForecastsAsync_WhenOpenMeteoBatchFails_ShouldPersistYrOnlyRows()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var location = TestDataBuilders.ParaglidingLocation()
            .WithId(locationId)
            .WithCoordinates(60.1234f, 10.5678f)
            .Build();

        var yrDto = TestDataBuilders.MetYrDto()
            .WithTime("2024-01-01T12:00:00Z")
            .WithSymbolCode("clearsky_day")
            .Build();

        var yrModel = new WeatherDataYr
        {
            MetYrDto = new[] { yrDto },
            UpdatedAt = "2024-01-01T12:00:00Z",
            Elevation = 100.0,
            Location = new LocationInfo { Latitude = 60.1234, Longitude = 10.5678 }
        };

        _paraglidingLocationServiceMock
            .Setup(x => x.GetLocationsWithoutForecastAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new LocationsWithoutForecast { LocationId = locationId } });

        _paraglidingLocationServiceMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { location });

        _metYrClientMock
            .Setup(x => x.FetchYrDataAsync(location.Latitude, location.Longitude, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<MetYrResponse>());

        _metYrMappingMock
            .Setup(x => x.MapYrData(It.IsAny<MetYrResponse>()))
            .Returns(yrModel);

        _openMeteoClientMock
            .Setup(x => x.FetchForecastAsync(It.IsAny<IReadOnlyList<OpenMeteoRequestLocation>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("batch failure"));

        _forecastCacheServiceMock
            .Setup(x => x.DeleteOldForecastsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        ForecastCache[]? capturedForecastCache = null;
        _forecastCacheServiceMock
            .Setup(x => x.UpsertManyAsync(It.IsAny<ForecastCache[]>(), It.IsAny<CancellationToken>()))
            .Callback<ForecastCache[], CancellationToken>((fc, _) => capturedForecastCache = fc)
            .ReturnsAsync(1);

        // Act
        await _service.UpdateForecastsAsync(TestContext.Current.CancellationToken);

        // Assert
        capturedForecastCache.Should().NotBeNull();
        capturedForecastCache.Should().HaveCount(1);
        capturedForecastCache![0].IsYrData.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateForecastsAsync_WhenOpenMeteoResponseCountDoesNotMatchSelectedLocations_ShouldPersistYrOnlyRows()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var location = TestDataBuilders.ParaglidingLocation()
            .WithId(locationId)
            .WithCoordinates(60.1234f, 10.5678f)
            .Build();

        var yrDto = TestDataBuilders.MetYrDto()
            .WithTime("2024-01-01T12:00:00Z")
            .WithSymbolCode("clearsky_day")
            .Build();

        var yrModel = new WeatherDataYr
        {
            MetYrDto = new[] { yrDto },
            UpdatedAt = "2024-01-01T12:00:00Z",
            Elevation = 100.0,
            Location = new LocationInfo { Latitude = 60.1234, Longitude = 10.5678 }
        };

        _paraglidingLocationServiceMock
            .Setup(x => x.GetLocationsWithoutForecastAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new LocationsWithoutForecast { LocationId = locationId } });

        _paraglidingLocationServiceMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { location });

        _metYrClientMock
            .Setup(x => x.FetchYrDataAsync(location.Latitude, location.Longitude, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<MetYrResponse>());

        _metYrMappingMock
            .Setup(x => x.MapYrData(It.IsAny<MetYrResponse>()))
            .Returns(yrModel);

        _openMeteoMappingMock
            .Setup(x => x.MapForecasts(It.IsAny<IReadOnlyList<OpenMeteoForecastResponse>>()))
            .Returns(Array.Empty<OpenMeteoLocationForecast>());

        _forecastCacheServiceMock
            .Setup(x => x.DeleteOldForecastsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        ForecastCache[]? capturedForecastCache = null;
        _forecastCacheServiceMock
            .Setup(x => x.UpsertManyAsync(It.IsAny<ForecastCache[]>(), It.IsAny<CancellationToken>()))
            .Callback<ForecastCache[], CancellationToken>((fc, _) => capturedForecastCache = fc)
            .ReturnsAsync(1);

        // Act
        await _service.UpdateForecastsAsync(TestContext.Current.CancellationToken);

        // Assert
        capturedForecastCache.Should().NotBeNull();
        capturedForecastCache.Should().HaveCount(1);
        capturedForecastCache![0].IsYrData.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateForecastsAsync_WhenOpenMeteoCoordinatesDoNotMatchSelectedLocations_ShouldUseRequestOrderForSupplementalRows()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var location = TestDataBuilders.ParaglidingLocation()
            .WithId(locationId)
            .WithCoordinates(60.1234f, 10.5678f)
            .Build();

        var yrDto = TestDataBuilders.MetYrDto()
            .WithTime("2024-01-01T12:00:00Z")
            .WithAirTemperature(12.3m)
            .WithSymbolCode("clearsky_day")
            .Build();

        var yrModel = new WeatherDataYr
        {
            MetYrDto = new[] { yrDto },
            UpdatedAt = "2024-01-01T12:00:00Z",
            Elevation = 100.0,
            Location = new LocationInfo { Latitude = 60.1234, Longitude = 10.5678 }
        };

        _paraglidingLocationServiceMock
            .Setup(x => x.GetLocationsWithoutForecastAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new LocationsWithoutForecast { LocationId = locationId } });

        _paraglidingLocationServiceMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { location });

        _metYrClientMock
            .Setup(x => x.FetchYrDataAsync(location.Latitude, location.Longitude, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<MetYrResponse>());

        _metYrMappingMock
            .Setup(x => x.MapYrData(It.IsAny<MetYrResponse>()))
            .Returns(yrModel);

        _openMeteoMappingMock
            .Setup(x => x.MapForecasts(It.IsAny<IReadOnlyList<OpenMeteoForecastResponse>>()))
            .Returns(new[]
            {
                new OpenMeteoLocationForecast
                {
                    Latitude = 60.999,
                    Longitude = 10.999,
                    Forecasts = new[]
                    {
                        new OpenMeteoForecastPoint
                        {
                            Time = DateTime.Parse("2024-01-01T13:00:00Z", null, DateTimeStyles.AdjustToUniversal),
                            Temperature = 10.1m,
                            WindSpeed = 6.2m,
                            WindDirection = 190,
                            WindGusts = 7.3m,
                            Precipitation = 0.25m,
                            PrecipitationProbability = 25f,
                            PressureMsl = 1010.1m,
                            WeatherCode = "cloudy",
                            IsDay = 1
                        }
                    }
                }
            });

        _forecastCacheServiceMock
            .Setup(x => x.DeleteOldForecastsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        ForecastCache[]? capturedForecastCache = null;
        _forecastCacheServiceMock
            .Setup(x => x.UpsertManyAsync(It.IsAny<ForecastCache[]>(), It.IsAny<CancellationToken>()))
            .Callback<ForecastCache[], CancellationToken>((fc, _) => capturedForecastCache = fc)
            .ReturnsAsync(2);

        // Act
        await _service.UpdateForecastsAsync(TestContext.Current.CancellationToken);

        // Assert
        capturedForecastCache.Should().NotBeNull();
        capturedForecastCache.Should().HaveCount(2);
        capturedForecastCache![0].IsYrData.Should().BeTrue();
        capturedForecastCache[1].IsYrData.Should().BeFalse();
        capturedForecastCache[1].Time.Should().Be(DateTime.Parse("2024-01-01T13:00:00Z", null, DateTimeStyles.AdjustToUniversal));
        capturedForecastCache[1].WindGusts.Should().BeNull();
    }
}


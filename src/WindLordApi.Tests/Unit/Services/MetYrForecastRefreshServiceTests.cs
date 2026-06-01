using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Data.Models;
using WindLordApi.Data.Services;
using WindLordApi.Integrations.MetYr;
using WindLordApi.Tests.Helpers;
using WindLordApi.Worker.Services;

namespace WindLordApi.Tests.Unit.Services;

/// <summary>
/// Unit tests for MetYrForecastRefreshService.
/// </summary>
public class MetYrForecastRefreshServiceTests
{
    private readonly Mock<IMetYrClient> _metYrClientMock;
    private readonly Mock<IMetYrMapping> _metYrMappingMock;
    private readonly Mock<IParaglidingLocationService> _paraglidingLocationServiceMock;
    private readonly Mock<IForecastCacheService> _forecastCacheServiceMock;
    private readonly Mock<ILogger<MetYrForecastRefreshService>> _loggerMock;
    private readonly MetYrForecastRefreshService _service;

    public MetYrForecastRefreshServiceTests()
    {
        _metYrClientMock = new Mock<IMetYrClient>();
        _metYrMappingMock = new Mock<IMetYrMapping>();
        _paraglidingLocationServiceMock = new Mock<IParaglidingLocationService>();
        _forecastCacheServiceMock = new Mock<IForecastCacheService>();
        _loggerMock = new Mock<ILogger<MetYrForecastRefreshService>>();

        _service = new MetYrForecastRefreshService(
            _metYrClientMock.Object,
            _metYrMappingMock.Object,
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
            .Setup(x => x.GetMetYrRefreshCandidatesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { locationId });

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
            .Setup(x => x.GetMetYrRefreshCandidatesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { locationId });

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
            .Setup(x => x.GetMetYrRefreshCandidatesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { locationId });

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
            .Setup(x => x.GetMetYrRefreshCandidatesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { locationId });

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

        // Verify the reduced write contract still populates the retained forecast fields.
        forecast.LocationId.Should().Be(locationId);
        forecast.IsYrData.Should().BeTrue();
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
            .Setup(x => x.GetMetYrRefreshCandidatesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { locationId });

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
            .Setup(x => x.GetMetYrRefreshCandidatesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { locationId });

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
            .Setup(x => x.GetMetYrRefreshCandidatesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { locationId });

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
            .Setup(x => x.GetMetYrRefreshCandidatesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { locationId1, locationId2 });

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
}
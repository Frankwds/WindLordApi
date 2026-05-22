using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Data.Models;
using WindLordApi.Data.Services;
using WindLordApi.Integrations.OpenMeteo;
using WindLordApi.Tests.Helpers;
using WindLordApi.Worker.Services;

namespace WindLordApi.Tests.Unit.Services;

public class OpenMeteoForecastSupplementServiceTests
{
    private readonly Mock<IOpenMeteoClient> _openMeteoClientMock;
    private readonly Mock<IOpenMeteoMapping> _openMeteoMappingMock;
    private readonly Mock<IParaglidingLocationService> _paraglidingLocationServiceMock;
    private readonly Mock<IForecastCacheService> _forecastCacheServiceMock;
    private readonly Mock<ILogger<OpenMeteoForecastSupplementService>> _loggerMock;
    private readonly OpenMeteoForecastSupplementService _service;

    public OpenMeteoForecastSupplementServiceTests()
    {
        _openMeteoClientMock = new Mock<IOpenMeteoClient>();
        _openMeteoMappingMock = new Mock<IOpenMeteoMapping>();
        _paraglidingLocationServiceMock = new Mock<IParaglidingLocationService>();
        _forecastCacheServiceMock = new Mock<IForecastCacheService>();
        _loggerMock = new Mock<ILogger<OpenMeteoForecastSupplementService>>();

        _service = new OpenMeteoForecastSupplementService(
            _openMeteoClientMock.Object,
            _openMeteoMappingMock.Object,
            _paraglidingLocationServiceMock.Object,
            _forecastCacheServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SupplementForecastsAsync_WhenLocationsLackOpenMeteoCoverage_ShouldBatchAndPersistSupplementalRows()
    {
        var locationId = Guid.NewGuid();
        var location = TestDataBuilders.ParaglidingLocation()
            .WithId(locationId)
            .WithCoordinates(60.1234f, 10.5678f)
            .Build();

        _paraglidingLocationServiceMock
            .Setup(x => x.GetOpenMeteoRefreshCandidatesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { locationId });

        _paraglidingLocationServiceMock
            .Setup(x => x.GetByIdsAsync(It.Is<List<Guid>>(ids => ids.Contains(locationId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { location });

        _openMeteoClientMock
            .Setup(x => x.FetchForecastAsync(It.IsAny<IReadOnlyList<OpenMeteoRequestLocation>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new OpenMeteoForecastResponse
                {
                    Latitude = OpenMeteoCoordinates.TruncateToRequestPrecision(location.Latitude),
                    Longitude = OpenMeteoCoordinates.TruncateToRequestPrecision(location.Longitude),
                    Hourly = new OpenMeteoHourlyForecast()
                }
            });

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
                            Time = DateTime.Parse("2024-01-03T13:00:00Z").ToUniversalTime(),
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

        ForecastCache[]? capturedForecastCache = null;
        _forecastCacheServiceMock
            .Setup(x => x.UpsertManyAsync(It.IsAny<ForecastCache[]>(), It.IsAny<CancellationToken>()))
            .Callback<ForecastCache[], CancellationToken>((forecastCache, _) => capturedForecastCache = forecastCache)
            .ReturnsAsync(1);

        await _service.SupplementForecastsAsync(TestContext.Current.CancellationToken);

        capturedForecastCache.Should().NotBeNull();
        capturedForecastCache.Should().HaveCount(1);
        capturedForecastCache![0].IsYrData.Should().BeFalse();
        capturedForecastCache[0].LandingWind.Should().BeNull();
        capturedForecastCache[0].WindGusts.Should().BeNull();
    }

    [Fact]
    public async Task SupplementForecastsAsync_ShouldSelectLocationsByShortestOpenMeteoForecastTail()
    {
        var firstLocationId = Guid.NewGuid();
        var secondLocationId = Guid.NewGuid();
        var selectedLocations = new[]
        {
            TestDataBuilders.ParaglidingLocation().WithId(firstLocationId).Build(),
            TestDataBuilders.ParaglidingLocation().WithId(secondLocationId).Build()
        };

        _paraglidingLocationServiceMock
            .Setup(x => x.GetOpenMeteoRefreshCandidatesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { firstLocationId, secondLocationId });

        _paraglidingLocationServiceMock
            .Setup(x => x.GetByIdsAsync(It.Is<List<Guid>>(ids => ids.Contains(firstLocationId) && ids.Contains(secondLocationId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(selectedLocations);

        _openMeteoClientMock
            .Setup(x => x.FetchForecastAsync(It.IsAny<IReadOnlyList<OpenMeteoRequestLocation>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(selectedLocations.Select(location => new OpenMeteoForecastResponse
            {
                Latitude = OpenMeteoCoordinates.TruncateToRequestPrecision(location.Latitude),
                Longitude = OpenMeteoCoordinates.TruncateToRequestPrecision(location.Longitude),
                Hourly = new OpenMeteoHourlyForecast()
            }).ToArray());

        _openMeteoMappingMock
            .Setup(x => x.MapForecasts(It.IsAny<IReadOnlyList<OpenMeteoForecastResponse>>()))
            .Returns(selectedLocations.Select(location => new OpenMeteoLocationForecast
            {
                Latitude = OpenMeteoCoordinates.TruncateToRequestPrecision(location.Latitude),
                Longitude = OpenMeteoCoordinates.TruncateToRequestPrecision(location.Longitude),
                Forecasts = Array.Empty<OpenMeteoForecastPoint>()
            }).ToArray());

        await _service.SupplementForecastsAsync(TestContext.Current.CancellationToken);

        _paraglidingLocationServiceMock.Verify(
            x => x.GetOpenMeteoRefreshCandidatesAsync(It.Is<int>(limit => limit == 50), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SupplementForecastsAsync_WhenResponseCountDoesNotMatchSelectedLocations_ShouldSkipPersistence()
    {
        var locationId = Guid.NewGuid();
        var location = TestDataBuilders.ParaglidingLocation().WithId(locationId).Build();

        _paraglidingLocationServiceMock
            .Setup(x => x.GetOpenMeteoRefreshCandidatesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { locationId });

        _paraglidingLocationServiceMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { location });

        _openMeteoClientMock
            .Setup(x => x.FetchForecastAsync(It.IsAny<IReadOnlyList<OpenMeteoRequestLocation>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new OpenMeteoForecastResponse(), new OpenMeteoForecastResponse() });

        _openMeteoMappingMock
            .Setup(x => x.MapForecasts(It.IsAny<IReadOnlyList<OpenMeteoForecastResponse>>()))
            .Returns(new[]
            {
                new OpenMeteoLocationForecast
                {
                    Latitude = 60.123,
                    Longitude = 10.567,
                    Forecasts = Array.Empty<OpenMeteoForecastPoint>()
                },
                new OpenMeteoLocationForecast
                {
                    Latitude = 60.456,
                    Longitude = 10.789,
                    Forecasts = Array.Empty<OpenMeteoForecastPoint>()
                }
            });

        await _service.SupplementForecastsAsync(TestContext.Current.CancellationToken);

        _forecastCacheServiceMock.Verify(x => x.UpsertManyAsync(It.IsAny<ForecastCache[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
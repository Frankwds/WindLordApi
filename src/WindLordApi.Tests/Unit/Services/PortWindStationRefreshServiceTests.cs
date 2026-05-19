using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Data.Models;
using WindLordApi.Data.Services;
using WindLordApi.Integrations.PortWind;
using WindLordApi.Worker.Services;

namespace WindLordApi.Tests.Unit.Services;

public class PortWindStationRefreshServiceTests
{
    private readonly Mock<IPortWindClient> _portWindClientMock;
    private readonly Mock<IPortWindMapping> _portWindMappingMock;
    private readonly Mock<IWeatherStationService> _weatherStationServiceMock;
    private readonly Mock<ILogger<PortWindStationRefreshService>> _loggerMock;
    private readonly PortWindStationRefreshService _service;

    public PortWindStationRefreshServiceTests()
    {
        _portWindClientMock = new Mock<IPortWindClient>();
        _portWindMappingMock = new Mock<IPortWindMapping>();
        _weatherStationServiceMock = new Mock<IWeatherStationService>();
        _loggerMock = new Mock<ILogger<PortWindStationRefreshService>>();

        _service = new PortWindStationRefreshService(
            _portWindClientMock.Object,
            _portWindMappingMock.Object,
            _weatherStationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SyncWeatherStationsAsync_AppliesProviderAuthoritativeStateUpdates()
    {
        // Arrange
        var providerStations = new Dictionary<string, PortWindStationCatalogEntry>
        {
            ["pw-1"] = new(),
            ["pw-2"] = new()
        };

        var weatherStations = new[]
        {
            new WeatherStation { StationId = "pw-1", Name = "Primary", Provider = PortWindOptions.ProviderName, Latitude = 60m, Longitude = 10m, Altitude = 0, IsActive = true },
            new WeatherStation { StationId = "pw-2", Name = "Secondary", Provider = PortWindOptions.ProviderName, Latitude = 61m, Longitude = 11m, Altitude = 0, IsActive = false }
        };

        var refreshResult = new PortWindStationRefreshResult
        {
            WeatherStations = weatherStations,
            SeenStationIds = new[] { "pw-1", "pw-2" },
            ActiveStationIds = new[] { "pw-1" },
            InactiveStationIds = new[] { "pw-2" }
        };

        _portWindClientMock
            .Setup(client => client.FetchStationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerStations);

        _portWindMappingMock
            .Setup(mapping => mapping.MapToStationRefreshResult(providerStations))
            .Returns(refreshResult);

        _weatherStationServiceMock
            .Setup(service => service.UpsertManyAsync(It.IsAny<WeatherStation[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(weatherStations.Length);

        _weatherStationServiceMock
            .Setup(service => service.SetStationsActiveByProviderAsync(PortWindOptions.ProviderName, refreshResult.ActiveStationIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _weatherStationServiceMock
            .Setup(service => service.SetStationsInactiveByProviderAsync(PortWindOptions.ProviderName, refreshResult.InactiveStationIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _weatherStationServiceMock
            .Setup(service => service.SetMissingStationsInactiveByProviderAsync(PortWindOptions.ProviderName, refreshResult.SeenStationIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _service.SyncWeatherStationsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(2);
        _weatherStationServiceMock.Verify(service => service.UpsertManyAsync(It.Is<WeatherStation[]>(stations => stations.Length == 2), It.IsAny<CancellationToken>()), Times.Once);
        _weatherStationServiceMock.Verify(service => service.SetStationsActiveByProviderAsync(PortWindOptions.ProviderName, refreshResult.ActiveStationIds, It.IsAny<CancellationToken>()), Times.Once);
        _weatherStationServiceMock.Verify(service => service.SetStationsInactiveByProviderAsync(PortWindOptions.ProviderName, refreshResult.InactiveStationIds, It.IsAny<CancellationToken>()), Times.Once);
        _weatherStationServiceMock.Verify(service => service.SetMissingStationsInactiveByProviderAsync(PortWindOptions.ProviderName, refreshResult.SeenStationIds, It.IsAny<CancellationToken>()), Times.Once);
    }
}

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
    public async Task SyncWeatherStationsAsync_WhenStationListParsingFails_ThrowsAndDoesNotApplyPartialChanges()
    {
        // Arrange
        _portWindClientMock
            .Setup(client => client.FetchStationsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FormatException("Malformed station list"));

        // Act
        var act = () => _service.SyncWeatherStationsAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<FormatException>();
        _weatherStationServiceMock.Verify(service => service.UpsertManyAsync(It.IsAny<WeatherStation[]>(), It.IsAny<CancellationToken>()), Times.Never);
        _weatherStationServiceMock.Verify(service => service.SetStationsActiveByProviderAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        _weatherStationServiceMock.Verify(service => service.SetStationsInactiveByProviderExceptAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncWeatherStationsAsync_WithMappedStations_OnlyReactivatesStationsMarkedActive()
    {
        // Arrange
        var providerStations = new Dictionary<string, PortWindStationDto>
        {
            ["VS1285"] = new(),
            ["VS1286"] = new()
        };
        var mappedStations = new List<WeatherStation>
        {
            new() { StationId = "VS1285", Name = "Tromso", Provider = PortWindOptions.ProviderName, IsActive = true, Latitude = 1, Longitude = 1 },
            new() { StationId = "VS1286", Name = "Bodo", Provider = PortWindOptions.ProviderName, IsActive = false, Latitude = 2, Longitude = 2 }
        };

        _portWindClientMock
            .Setup(client => client.FetchStationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerStations);
        _portWindMappingMock
            .Setup(mapping => mapping.MapStations(providerStations))
            .Returns(mappedStations);

        // Act
        var result = await _service.SyncWeatherStationsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(2);
        _weatherStationServiceMock.Verify(service => service.UpsertManyAsync(
            It.Is<WeatherStation[]>(stations => stations.Length == 2 && stations.All(station => station.Provider == PortWindOptions.ProviderName)),
            It.IsAny<CancellationToken>()), Times.Once);
        _weatherStationServiceMock.Verify(service => service.SetStationsActiveByProviderAsync(
            PortWindOptions.ProviderName,
            It.Is<IEnumerable<string>>(stationIds => stationIds.SequenceEqual(new[] { "VS1285" })),
            It.IsAny<CancellationToken>()), Times.Once);
        _weatherStationServiceMock.Verify(service => service.SetStationsInactiveByProviderExceptAsync(
            PortWindOptions.ProviderName,
            It.Is<IEnumerable<string>>(stationIds => stationIds.SequenceEqual(new[] { "VS1285" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
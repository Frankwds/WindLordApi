using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Data.Models;
using WindLordApi.Data.Services;
using WindLordApi.Integrations.PortWind;
using WindLordApi.Worker.Services;

namespace WindLordApi.Tests.Unit.Services;

public class PortWindLatestDataSyncServiceTests
{
    private readonly Mock<IPortWindClient> _portWindClientMock;
    private readonly Mock<IPortWindMapping> _portWindMappingMock;
    private readonly Mock<IWeatherStationService> _weatherStationServiceMock;
    private readonly Mock<IStationDataService> _stationDataServiceMock;
    private readonly Mock<ILatestStationDataService> _latestStationDataServiceMock;
    private readonly Mock<ILogger<PortWindLatestDataSyncService>> _loggerMock;
    private readonly PortWindLatestDataSyncService _service;

    public PortWindLatestDataSyncServiceTests()
    {
        _portWindClientMock = new Mock<IPortWindClient>();
        _portWindMappingMock = new Mock<IPortWindMapping>();
        _weatherStationServiceMock = new Mock<IWeatherStationService>();
        _stationDataServiceMock = new Mock<IStationDataService>();
        _latestStationDataServiceMock = new Mock<ILatestStationDataService>();
        _loggerMock = new Mock<ILogger<PortWindLatestDataSyncService>>();

        _service = new PortWindLatestDataSyncService(
            _portWindClientMock.Object,
            _portWindMappingMock.Object,
            _weatherStationServiceMock.Object,
            _stationDataServiceMock.Object,
            _latestStationDataServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SyncLatestStationDataAsync_ContinuesAfterPerStationFailures()
    {
        // Arrange
        var successfulResponse = new PortWindLatestResponse
        {
            LastMeasurement = 1732968000000L,
            Data = new[] { new PortWindLatestDataPoint { WindSpeedAverage = 5m, WindDirectionAverage = 180m } }
        };
        var emptyResponse = new PortWindLatestResponse { LastMeasurement = 1732968005000L };
        var stationData = new StationData
        {
            StationId = "pw-1",
            WindSpeed = 5m,
            Direction = 180,
            UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(1732968000000L).UtcDateTime,
            IsCompressed = false
        };

        _weatherStationServiceMock
            .Setup(service => service.GetActiveStationIdsByProviderAsync(PortWindOptions.ProviderName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "pw-1", "pw-2", "pw-3" });

        _portWindClientMock
            .Setup(client => client.FetchLatestDataAsync("pw-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(successfulResponse);
        _portWindClientMock
            .Setup(client => client.FetchLatestDataAsync("pw-2", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("upstream failure"));
        _portWindClientMock
            .Setup(client => client.FetchLatestDataAsync("pw-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyResponse);

        _portWindMappingMock
            .Setup(mapping => mapping.MapToStationData("pw-1", successfulResponse))
            .Returns(stationData);
        _portWindMappingMock
            .Setup(mapping => mapping.MapToStationData("pw-3", emptyResponse))
            .Returns((StationData?)null);

        _stationDataServiceMock
            .Setup(service => service.UpsertManyAsync(It.IsAny<StationData[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _latestStationDataServiceMock
            .Setup(service => service.UpsertManyAsync(It.IsAny<LatestStationData[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var insertedCount = await _service.SyncLatestStationDataAsync(TestContext.Current.CancellationToken);

        // Assert
        insertedCount.Should().Be(1);
        _portWindClientMock.Verify(client => client.FetchLatestDataAsync("pw-1", It.IsAny<CancellationToken>()), Times.Once);
        _portWindClientMock.Verify(client => client.FetchLatestDataAsync("pw-2", It.IsAny<CancellationToken>()), Times.Once);
        _portWindClientMock.Verify(client => client.FetchLatestDataAsync("pw-3", It.IsAny<CancellationToken>()), Times.Once);
        _stationDataServiceMock.Verify(service => service.UpsertManyAsync(It.Is<StationData[]>(data => data.Length == 1 && data[0].StationId == "pw-1"), It.IsAny<CancellationToken>()), Times.Once);
        _latestStationDataServiceMock.Verify(service => service.UpsertManyAsync(It.Is<LatestStationData[]>(data => data.Length == 1 && data[0].StationId == "pw-1"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncLatestStationDataAsync_GivenInvalidJsonForStation_SetsStationInactiveAndContinues()
    {
        // Arrange
        var successfulResponse = new PortWindLatestResponse
        {
            LastMeasurement = 1732968000000L,
            Data = new[] { new PortWindLatestDataPoint { WindSpeedAverage = 5m, WindDirectionAverage = 180m } }
        };
        var stationData = new StationData
        {
            StationId = "pw-2",
            WindSpeed = 5m,
            Direction = 180,
            UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(1732968000000L).UtcDateTime,
            IsCompressed = false
        };

        _weatherStationServiceMock
            .Setup(service => service.GetActiveStationIdsByProviderAsync(PortWindOptions.ProviderName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "pw-1", "pw-2" });

        _portWindClientMock
            .Setup(client => client.FetchLatestDataAsync("pw-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JsonException("invalid payload"));
        _portWindClientMock
            .Setup(client => client.FetchLatestDataAsync("pw-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(successfulResponse);

        _weatherStationServiceMock
            .Setup(service => service.SetStationsInactiveByProviderAsync(PortWindOptions.ProviderName, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _portWindMappingMock
            .Setup(mapping => mapping.MapToStationData("pw-2", successfulResponse))
            .Returns(stationData);

        _stationDataServiceMock
            .Setup(service => service.UpsertManyAsync(It.IsAny<StationData[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _latestStationDataServiceMock
            .Setup(service => service.UpsertManyAsync(It.IsAny<LatestStationData[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var insertedCount = await _service.SyncLatestStationDataAsync(TestContext.Current.CancellationToken);

        // Assert
        insertedCount.Should().Be(1);
        _weatherStationServiceMock.Verify(
            service => service.SetStationsInactiveByProviderAsync(
                PortWindOptions.ProviderName,
                It.Is<IEnumerable<string>>(stationIds => stationIds.SequenceEqual(new[] { "pw-1" })),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _stationDataServiceMock.Verify(service => service.UpsertManyAsync(It.Is<StationData[]>(data => data.Length == 1 && data[0].StationId == "pw-2"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
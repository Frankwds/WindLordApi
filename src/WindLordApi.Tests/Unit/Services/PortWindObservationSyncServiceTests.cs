using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Data.Models;
using WindLordApi.Data.Services;
using WindLordApi.Integrations.PortWind;
using WindLordApi.Worker.Services;

namespace WindLordApi.Tests.Unit.Services;

public class PortWindObservationSyncServiceTests
{
    private readonly Mock<IPortWindClient> _portWindClientMock;
    private readonly Mock<IPortWindMapping> _portWindMappingMock;
    private readonly Mock<IWeatherStationService> _weatherStationServiceMock;
    private readonly Mock<IStationDataService> _stationDataServiceMock;
    private readonly Mock<ILatestStationDataService> _latestStationDataServiceMock;
    private readonly Mock<ILogger<PortWindObservationSyncService>> _loggerMock;
    private readonly PortWindObservationSyncService _service;

    public PortWindObservationSyncServiceTests()
    {
        _portWindClientMock = new Mock<IPortWindClient>();
        _portWindMappingMock = new Mock<IPortWindMapping>();
        _weatherStationServiceMock = new Mock<IWeatherStationService>();
        _stationDataServiceMock = new Mock<IStationDataService>();
        _latestStationDataServiceMock = new Mock<ILatestStationDataService>();
        _loggerMock = new Mock<ILogger<PortWindObservationSyncService>>();

        var fetchSequence = new MockSequence();
        _portWindClientMock.InSequence(fetchSequence)
            .Setup(client => client.FetchLatestAndPreviousObservationAsync("VS1285", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PortWindObservationResponseDto
            {
                Data =
                [
                    new PortWindObservationDto { Uts = 1716037200000 }
                ]
            });
        _portWindClientMock.InSequence(fetchSequence)
            .Setup(client => client.FetchLatestAndPreviousObservationAsync("VS1286", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Transient error"));
        _portWindClientMock.InSequence(fetchSequence)
            .Setup(client => client.FetchLatestAndPreviousObservationAsync("VS1287", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PortWindObservationResponseDto());

        _service = new PortWindObservationSyncService(
            _portWindClientMock.Object,
            _portWindMappingMock.Object,
            _weatherStationServiceMock.Object,
            _stationDataServiceMock.Object,
            _latestStationDataServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SyncLatestStationDataAsync_ContinuesOnPerStationFailures_AndSkipsEmptyResponses()
    {
        // Arrange
        _weatherStationServiceMock
            .Setup(service => service.GetActiveStationIdsByProviderAsync(PortWindOptions.ProviderName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "VS1285", "VS1286", "VS1287" });

        var mappedStationData = new List<StationData>
        {
            new()
            {
                StationId = "VS1285",
                WindSpeed = 8.4m,
                Direction = 270,
                UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(1716037200000).UtcDateTime
            }
        };

        _portWindMappingMock
            .Setup(mapping => mapping.MapObservations("VS1285", It.IsAny<IReadOnlyList<PortWindObservationDto>>()))
            .Returns(mappedStationData);
        _portWindMappingMock
            .Setup(mapping => mapping.MapObservations("VS1287", It.IsAny<IReadOnlyList<PortWindObservationDto>>()))
            .Returns([]);

        _stationDataServiceMock
            .Setup(service => service.UpsertManyAsync(It.IsAny<StationData[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.SyncLatestStationDataAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);
        _portWindClientMock.Verify(client => client.FetchLatestAndPreviousObservationAsync("VS1285", It.IsAny<CancellationToken>()), Times.Once);
        _portWindClientMock.Verify(client => client.FetchLatestAndPreviousObservationAsync("VS1286", It.IsAny<CancellationToken>()), Times.Once);
        _portWindClientMock.Verify(client => client.FetchLatestAndPreviousObservationAsync("VS1287", It.IsAny<CancellationToken>()), Times.Once);
        _stationDataServiceMock.Verify(service => service.UpsertManyAsync(
            It.Is<StationData[]>(rows => rows.Length == 1 && rows[0].StationId == "VS1285"),
            It.IsAny<CancellationToken>()), Times.Once);
        _latestStationDataServiceMock.Verify(service => service.UpsertManyAsync(
            It.Is<LatestStationData[]>(rows => rows.Length == 1 && rows[0].StationId == "VS1285"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Data.Services;
using WindLordApi.Worker.Services;
using Xunit;

namespace WindLordApi.Tests.Unit.Services;

public class StationDataRetentionServiceTests
{
    private readonly Mock<IStationDataService> _stationDataServiceMock;
    private readonly Mock<ILogger<StationDataRetentionService>> _loggerMock;
    private readonly StationDataRetentionService _service;

    public StationDataRetentionServiceTests()
    {
        _stationDataServiceMock = new Mock<IStationDataService>();
        _loggerMock = new Mock<ILogger<StationDataRetentionService>>();
        _service = new StationDataRetentionService(_stationDataServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CleanupOldObservationsAsync_DeletesWithTwentyFourHourRetention()
    {
        _stationDataServiceMock
            .Setup(service => service.DeleteOlderThanAsync(TimeSpan.FromHours(24), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var deletedCount = await _service.CleanupOldObservationsAsync(TestContext.Current.CancellationToken);

        deletedCount.Should().Be(42);
        _stationDataServiceMock.Verify(
            service => service.DeleteOlderThanAsync(TimeSpan.FromHours(24), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

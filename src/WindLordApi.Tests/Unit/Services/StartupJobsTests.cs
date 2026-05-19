using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Worker.Services;
using WindLordApi.Worker.Startup;

namespace WindLordApi.Tests.Unit.Services;

public class StartupJobsTests
{
    [Fact]
    public async Task RunStartupJobsAsync_RunsPortWindStationRefreshBeforePortWindLatestData()
    {
        // Arrange
        var executionOrder = new List<string>();

        var windsMobiSyncServiceMock = new Mock<IWindsMobiSyncService>();
        windsMobiSyncServiceMock
            .Setup(service => service.SyncWindsMobiDataAsync(It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("WindsMobi"))
            .ReturnsAsync(0);

        var countryLocatorServiceMock = new Mock<ICountryLocatorService>();
        countryLocatorServiceMock
            .Setup(service => service.LocateCountriesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("CountryLocator"))
            .ReturnsAsync(0);

        var forecastUpdateServiceMock = new Mock<IForecastUpdateService>();
        forecastUpdateServiceMock
            .Setup(service => service.UpdateForecastsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("ForecastUpdate"))
            .Returns(Task.CompletedTask);

        var holfuySyncServiceMock = new Mock<IHolfuySyncService>();
        holfuySyncServiceMock
            .Setup(service => service.SyncHolfuyDataAsync(It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("Holfuy"))
            .ReturnsAsync(0);

        var portWindStationRefreshServiceMock = new Mock<IPortWindStationRefreshService>();
        portWindStationRefreshServiceMock
            .Setup(service => service.SyncWeatherStationsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("PortWindRefresh"))
            .ReturnsAsync(0);

        var portWindLatestDataSyncServiceMock = new Mock<IPortWindLatestDataSyncService>();
        portWindLatestDataSyncServiceMock
            .Setup(service => service.SyncLatestStationDataAsync(It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("PortWindLatest"))
            .ReturnsAsync(0);

        var metFrostSyncServiceMock = new Mock<IMetFrostSyncService>();
        metFrostSyncServiceMock
            .Setup(service => service.SyncLatestStationDataAsync(It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("MetFrostLatest"))
            .ReturnsAsync(0);
        metFrostSyncServiceMock
            .Setup(service => service.SyncWeatherStationsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("MetFrostStations"))
            .ReturnsAsync(0);
        metFrostSyncServiceMock
            .Setup(service => service.SyncWeatherStationsActiveStatusAsync(It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("MetFrostStatus"))
            .ReturnsAsync(0);

        var services = new ServiceCollection();
        services.AddSingleton(windsMobiSyncServiceMock.Object);
        services.AddSingleton(countryLocatorServiceMock.Object);
        services.AddSingleton(forecastUpdateServiceMock.Object);
        services.AddSingleton(holfuySyncServiceMock.Object);
        services.AddSingleton(portWindStationRefreshServiceMock.Object);
        services.AddSingleton(portWindLatestDataSyncServiceMock.Object);
        services.AddSingleton(metFrostSyncServiceMock.Object);

        using var serviceProvider = services.BuildServiceProvider();
        var loggerMock = new Mock<ILogger>();

        // Act
        await StartupJobs.RunStartupJobsAsync(serviceProvider, loggerMock.Object, TestContext.Current.CancellationToken);

        // Assert
        executionOrder.Should().Contain("PortWindRefresh");
        executionOrder.Should().Contain("PortWindLatest");
        executionOrder.IndexOf("PortWindRefresh").Should().BeLessThan(executionOrder.IndexOf("PortWindLatest"));
    }
}
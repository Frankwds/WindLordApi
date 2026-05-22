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
    public async Task RunStartupJobsAsync_ShouldRunOpenMeteoBeforeMetYr()
    {
        var executionOrder = new List<string>();

        var openMeteoServiceMock = new Mock<IOpenMeteoForecastSupplementService>();
        openMeteoServiceMock
            .Setup(service => service.SupplementForecastsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("OpenMeteo"))
            .Returns(Task.CompletedTask);

        var metYrServiceMock = new Mock<IMetYrForecastRefreshService>();
        metYrServiceMock
            .Setup(service => service.UpdateForecastsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("MetYr"))
            .Returns(Task.CompletedTask);

        var portWindStationRefreshServiceMock = new Mock<IPortWindStationRefreshService>();
        portWindStationRefreshServiceMock
            .Setup(service => service.SyncWeatherStationsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("PortWindStations"))
            .ReturnsAsync(0);

        var portWindLatestDataServiceMock = new Mock<IPortWindLatestDataSyncService>();
        portWindLatestDataServiceMock
            .Setup(service => service.SyncLatestStationDataAsync(It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("PortWindLatest"))
            .ReturnsAsync(0);

        var windsMobiServiceMock = new Mock<IWindsMobiSyncService>();
        windsMobiServiceMock
            .Setup(service => service.SyncWindsMobiDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var countryLocatorServiceMock = new Mock<ICountryLocatorService>();
        countryLocatorServiceMock
            .Setup(service => service.LocateCountriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var holfuyServiceMock = new Mock<IHolfuySyncService>();
        holfuyServiceMock
            .Setup(service => service.SyncHolfuyDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var metFrostServiceMock = new Mock<IMetFrostSyncService>();
        metFrostServiceMock
            .Setup(service => service.SyncLatestStationDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        metFrostServiceMock
            .Setup(service => service.SyncWeatherStationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        metFrostServiceMock
            .Setup(service => service.SyncWeatherStationsActiveStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var serviceProvider = BuildServiceProvider(
            openMeteoServiceMock,
            metYrServiceMock,
            portWindStationRefreshServiceMock,
            portWindLatestDataServiceMock,
            windsMobiServiceMock,
            countryLocatorServiceMock,
            holfuyServiceMock,
            metFrostServiceMock);

        var loggerMock = new Mock<ILogger>();

        await StartupJobs.RunStartupJobsAsync(serviceProvider, loggerMock.Object, TestContext.Current.CancellationToken);

        executionOrder.Should().ContainInOrder("OpenMeteo", "MetYr", "PortWindStations", "PortWindLatest");
    }

    [Fact]
    public async Task RunStartupJobsAsync_WhenOpenMeteoFails_ShouldStillRunMetYr()
    {
        var openMeteoServiceMock = new Mock<IOpenMeteoForecastSupplementService>();
        openMeteoServiceMock
            .Setup(service => service.SupplementForecastsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Open-Meteo startup failure"));

        var metYrServiceMock = new Mock<IMetYrForecastRefreshService>();
        metYrServiceMock
            .Setup(service => service.UpdateForecastsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var portWindStationRefreshServiceMock = new Mock<IPortWindStationRefreshService>();
        portWindStationRefreshServiceMock
            .Setup(service => service.SyncWeatherStationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var portWindLatestDataServiceMock = new Mock<IPortWindLatestDataSyncService>();
        portWindLatestDataServiceMock
            .Setup(service => service.SyncLatestStationDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var windsMobiServiceMock = new Mock<IWindsMobiSyncService>();
        windsMobiServiceMock
            .Setup(service => service.SyncWindsMobiDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var countryLocatorServiceMock = new Mock<ICountryLocatorService>();
        countryLocatorServiceMock
            .Setup(service => service.LocateCountriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var holfuyServiceMock = new Mock<IHolfuySyncService>();
        holfuyServiceMock
            .Setup(service => service.SyncHolfuyDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var metFrostServiceMock = new Mock<IMetFrostSyncService>();
        metFrostServiceMock
            .Setup(service => service.SyncLatestStationDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        metFrostServiceMock
            .Setup(service => service.SyncWeatherStationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        metFrostServiceMock
            .Setup(service => service.SyncWeatherStationsActiveStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var serviceProvider = BuildServiceProvider(
            openMeteoServiceMock,
            metYrServiceMock,
            portWindStationRefreshServiceMock,
            portWindLatestDataServiceMock,
            windsMobiServiceMock,
            countryLocatorServiceMock,
            holfuyServiceMock,
            metFrostServiceMock);

        var loggerMock = new Mock<ILogger>();

        await StartupJobs.RunStartupJobsAsync(serviceProvider, loggerMock.Object, TestContext.Current.CancellationToken);

        metYrServiceMock.Verify(service => service.UpdateForecastsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ServiceProvider BuildServiceProvider(
        Mock<IOpenMeteoForecastSupplementService> openMeteoServiceMock,
        Mock<IMetYrForecastRefreshService> metYrServiceMock,
        Mock<IPortWindStationRefreshService> portWindStationRefreshServiceMock,
        Mock<IPortWindLatestDataSyncService> portWindLatestDataServiceMock,
        Mock<IWindsMobiSyncService> windsMobiServiceMock,
        Mock<ICountryLocatorService> countryLocatorServiceMock,
        Mock<IHolfuySyncService> holfuyServiceMock,
        Mock<IMetFrostSyncService> metFrostServiceMock)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => openMeteoServiceMock.Object);
        services.AddScoped(_ => metYrServiceMock.Object);
        services.AddScoped(_ => portWindStationRefreshServiceMock.Object);
        services.AddScoped(_ => portWindLatestDataServiceMock.Object);
        services.AddScoped(_ => windsMobiServiceMock.Object);
        services.AddScoped(_ => countryLocatorServiceMock.Object);
        services.AddScoped(_ => holfuyServiceMock.Object);
        services.AddScoped(_ => metFrostServiceMock.Object);
        return services.BuildServiceProvider();
    }
}
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Integrations.OpenMeteo;
using WindLordApi.Worker.Startup;

namespace WindLordApi.Tests.Unit.Services;

public class OpenMeteoHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_GivenAccessibleForecastEndpoint_ReturnsHealthy()
    {
        // Arrange
        var clientMock = new Mock<IOpenMeteoClient>();
        clientMock
            .Setup(x => x.FetchForecastAsync(It.IsAny<IReadOnlyList<OpenMeteoRequestLocation>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new OpenMeteoForecastResponse
                {
                    Latitude = 61.881,
                    Longitude = 9.103,
                    Hourly = new OpenMeteoHourlyForecast()
                }
            });

        var healthCheck = new OpenMeteoHealthCheck(clientMock.Object, Mock.Of<ILogger<OpenMeteoHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }
}
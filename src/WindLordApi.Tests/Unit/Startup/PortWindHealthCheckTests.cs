using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Integrations.PortWind;
using WindLordApi.Worker.Startup;

namespace WindLordApi.Tests.Unit.Startup;

public class PortWindHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_GivenAccessibleCatalog_ReturnsHealthy()
    {
        // Arrange
        var clientMock = new Mock<IPortWindClient>();
        clientMock
            .Setup(client => client.FetchStationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, PortWindStationCatalogEntry>
            {
                ["pw-1"] = new()
            });

        var healthCheck = new PortWindHealthCheck(clientMock.Object, Mock.Of<ILogger<PortWindHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("Parsed 1 station records");
    }
}
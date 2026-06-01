using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Worker.Startup;

namespace WindLordApi.Tests.Unit.Services;

public class HealthCheckTests
{
    [Fact]
    public async Task RunHealthChecksAsync_WhenSchemaTaggedCheckIsUnhealthy_ShouldThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddCheck<SchemaFailureHealthCheck>("forecast-cache-schema", tags: ["db", "schema"]);

        await using var serviceProvider = services.BuildServiceProvider();
        var loggerMock = new Mock<ILogger>();

        var action = async () => await HealthCheck.RunHealthChecksAsync(
            serviceProvider,
            loggerMock.Object,
            TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*forecast-cache-schema*schema mismatch*");
    }

    [Fact]
    public async Task RunHealthChecksAsync_WhenNonSchemaCheckIsUnhealthy_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddCheck<AdvisoryFailureHealthCheck>("metyr", tags: ["api", "metyr"]);

        await using var serviceProvider = services.BuildServiceProvider();
        var loggerMock = new Mock<ILogger>();

        var action = async () => await HealthCheck.RunHealthChecksAsync(
            serviceProvider,
            loggerMock.Object,
            TestContext.Current.CancellationToken);

        await action.Should().NotThrowAsync();
    }

    private sealed class SchemaFailureHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("schema mismatch"));
        }
    }

    private sealed class AdvisoryFailureHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("api unavailable"));
        }
    }
}
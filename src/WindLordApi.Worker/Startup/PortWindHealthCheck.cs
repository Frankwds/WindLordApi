using Microsoft.Extensions.Diagnostics.HealthChecks;
using WindLordApi.Integrations.PortWind;

namespace WindLordApi.Worker.Startup;

/// <summary>
/// Health check for PortWind station-list connectivity.
/// </summary>
public class PortWindHealthCheck : IHealthCheck
{
    private readonly IPortWindClient _portWindClient;
    private readonly ILogger<PortWindHealthCheck> _logger;

    public PortWindHealthCheck(
        IPortWindClient portWindClient,
        ILogger<PortWindHealthCheck> logger)
    {
        _portWindClient = portWindClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stations = await _portWindClient.FetchStationsAsync(cancellationToken);

            _logger.LogInformation(
                "PortWind health check passed. Parsed {StationCount} stations from the station list",
                stations.Count);

            return HealthCheckResult.Healthy(
                $"PortWind station list is accessible. Parsed {stations.Count} station records.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PortWind health check failed with exception");
            return HealthCheckResult.Unhealthy("PortWind health check failed", ex);
        }
    }
}
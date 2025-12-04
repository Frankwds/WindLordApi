using Microsoft.Extensions.Diagnostics.HealthChecks;
using WindLordApi.Integrations.Holfuy;

namespace WindLordApi.Worker.Startup;

/// <summary>
/// Health check for Holfuy API connectivity.
/// </summary>
public class HolfuyHealthCheck : IHealthCheck
{
    private readonly IHolfuyClient _holfuyClient;
    private readonly ILogger<HolfuyHealthCheck> _logger;

    public HolfuyHealthCheck(
        IHolfuyClient holfuyClient,
        ILogger<HolfuyHealthCheck> logger)
    {
        _holfuyClient = holfuyClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test API connectivity by fetching all available data (same as production)
            // This uses the same proxy configuration and fetches all stations
            var result = await _holfuyClient.FetchHolfuyDataAsync(cancellationToken);

            _logger.LogInformation(
                "Holfuy health check passed. Fetched {StationCount} stations, {DataCount} data records",
                result.WeatherStations.Count, result.StationData.Count);

            return HealthCheckResult.Healthy(
                $"Holfuy API is accessible. Fetched {result.WeatherStations.Count} stations and {result.StationData.Count} data records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Holfuy health check failed with exception");
            return HealthCheckResult.Unhealthy("Holfuy API health check failed", ex);
        }
    }
}


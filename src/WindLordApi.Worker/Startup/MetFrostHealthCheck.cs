using Microsoft.Extensions.Diagnostics.HealthChecks;
using WindLordApi.Integrations.MetFrost;

namespace WindLordApi.Worker.Startup;

/// <summary>
/// Health check for MetFrost API connectivity.
/// </summary>
public class MetFrostHealthCheck : IHealthCheck
{
    private readonly IMetFrostClient _metFrostClient;
    private readonly ILogger<MetFrostHealthCheck> _logger;

    // Hardcoded test station IDs as specified
    private static readonly string[] TestStationIds = ["SN60810", "SN39212", "SN97710"];

    public MetFrostHealthCheck(
        IMetFrostClient metFrostClient,
        ILogger<MetFrostHealthCheck> logger)
    {
        _metFrostClient = metFrostClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test API connectivity by fetching data for hardcoded stations
            var response = await _metFrostClient.FetchMetStationDataAsync(
                TestStationIds,
                cancellationToken);

            _logger.LogInformation(
                "MetFrost health check passed. Response received with {ItemCount} items",
                response.TotalItemCount);

            return HealthCheckResult.Healthy(
                $"MetFrost API is accessible. Response received with {response.TotalItemCount} items");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MetFrost health check failed with exception");
            return HealthCheckResult.Unhealthy("MetFrost API health check failed", ex);
        }
    }
}


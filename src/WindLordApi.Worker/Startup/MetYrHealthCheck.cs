using Microsoft.Extensions.Diagnostics.HealthChecks;
using WindLordApi.Integrations.MetYr;

namespace WindLordApi.Worker.Startup;

/// <summary>
/// Health check for MetYr API connectivity.
/// </summary>
public class MetYrHealthCheck : IHealthCheck
{
    private readonly IMetYrClient _metYrClient;
    private readonly ILogger<MetYrHealthCheck> _logger;

    // Test coordinates as specified
    private const double TestLatitude = 61.881054;
    private const double TestLongitude = 9.103082;

    public MetYrHealthCheck(
        IMetYrClient metYrClient,
        ILogger<MetYrHealthCheck> logger)
    {
        _metYrClient = metYrClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test API connectivity by fetching data for test coordinates
            var response = await _metYrClient.FetchYrDataAsync(
                TestLatitude,
                TestLongitude,
                cancellationToken);

            var timeseriesCount = response.Properties?.Timeseries?.Count ?? 0;

            _logger.LogInformation(
                "MetYr health check passed. Response received with {TimeseriesCount} timeseries entries",
                timeseriesCount);

            return HealthCheckResult.Healthy(
                $"MetYr API is accessible. Response received with {timeseriesCount} timeseries entries");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MetYr health check failed with exception");
            return HealthCheckResult.Unhealthy("MetYr API health check failed", ex);
        }
    }
}


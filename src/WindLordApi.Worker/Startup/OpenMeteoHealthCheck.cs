using Microsoft.Extensions.Diagnostics.HealthChecks;
using WindLordApi.Integrations.OpenMeteo;

namespace WindLordApi.Worker.Startup;

/// <summary>
/// Health check for OpenMeteo API connectivity.
/// </summary>
public class OpenMeteoHealthCheck : IHealthCheck
{
    private readonly IOpenMeteoClient _openMeteoClient;
    private readonly ILogger<OpenMeteoHealthCheck> _logger;

    // Test coordinates as specified
    private const float TestLatitude = 61.881054f;
    private const float TestLongitude = 9.103082f;

    public OpenMeteoHealthCheck(
        IOpenMeteoClient openMeteoClient,
        ILogger<OpenMeteoHealthCheck> logger)
    {
        _openMeteoClient = openMeteoClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test API connectivity by fetching data for test coordinates
            // OpenMeteo accepts arrays, so we pass single coordinate as arrays
            var responses = await _openMeteoClient.FetchMeteoDataAsync(
                [TestLatitude],
                [TestLongitude],
                cancellationToken);

            var response = responses.FirstOrDefault();
            var hourlyDataCount = response?.Hourly?.Time?.Count ?? 0;

            _logger.LogInformation(
                "OpenMeteo health check passed. Response received with {HourlyDataCount} hourly data entries",
                hourlyDataCount);

            return HealthCheckResult.Healthy(
                $"OpenMeteo API is accessible. Response received with {hourlyDataCount} hourly data entries");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenMeteo health check failed with exception");
            return HealthCheckResult.Unhealthy("OpenMeteo API health check failed", ex);
        }
    }
}


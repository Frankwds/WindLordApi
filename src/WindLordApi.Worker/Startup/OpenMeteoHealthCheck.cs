using Microsoft.Extensions.Diagnostics.HealthChecks;
using WindLordApi.Integrations.OpenMeteo;

namespace WindLordApi.Worker.Startup;

/// <summary>
/// Health check for Open-Meteo forecast connectivity.
/// </summary>
public class OpenMeteoHealthCheck : IHealthCheck
{
    private readonly IOpenMeteoClient _openMeteoClient;
    private readonly ILogger<OpenMeteoHealthCheck> _logger;

    private static readonly OpenMeteoRequestLocation TestLocation = new(61.881054, 9.103082);

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
            var now = DateTime.UtcNow;
            var responses = await _openMeteoClient.FetchForecastAsync(
                new[] { TestLocation },
                now.AddHours(48),
                now.AddHours(96),
                cancellationToken);

            var locationCount = responses.Count;
            _logger.LogInformation(
                "OpenMeteo health check passed. Response received for {LocationCount} location blocks",
                locationCount);

            return HealthCheckResult.Healthy(
                $"Open-Meteo forecast endpoint is accessible. Response received for {locationCount} location blocks.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenMeteo health check failed with exception");
            return HealthCheckResult.Unhealthy("Open-Meteo forecast health check failed", ex);
        }
    }
}
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WindLordApi.Worker.Startup;

/// <summary>
/// Startup health check runner that executes all registered health checks and logs results.
/// </summary>
public static class HealthCheck
{
    /// <summary>
    /// Runs all registered health checks and logs their status.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve health check services.</param>
    /// <param name="logger">The logger to use for logging health check results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    public static async Task RunHealthChecksAsync(
        IServiceProvider serviceProvider,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting health checks...");

        using var scope = serviceProvider.CreateScope();
        var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();

        // Run all health checks
        var healthReport = await healthCheckService.CheckHealthAsync(cancellationToken);

        // Log overall status
        var overallStatus = healthReport.Status;
        logger.LogInformation(
            "Health check completed. Overall status: {Status}. Total checks: {TotalChecks}, Duration: {Duration}ms",
            overallStatus,
            healthReport.Entries.Count,
            healthReport.TotalDuration.TotalMilliseconds);

        // Log individual health check results
        foreach (var entry in healthReport.Entries)
        {
            var status = entry.Value.Status;
            var duration = entry.Value.Duration.TotalMilliseconds;
            var description = entry.Value.Description ?? "No description";
            var exception = entry.Value.Exception;

            if (status == HealthStatus.Healthy)
            {
                logger.LogInformation(
                    "✓ {CheckName}: {Status} - {Description} (Duration: {Duration}ms)",
                    entry.Key,
                    status,
                    description,
                    duration);
            }
            else if (status == HealthStatus.Degraded)
            {
                logger.LogWarning(
                    "⚠ {CheckName}: {Status} - {Description} (Duration: {Duration}ms)",
                    entry.Key,
                    status,
                    description,
                    duration);
            }
            else
            {
                logger.LogError(
                    "✗ {CheckName}: {Status} - {Description} (Duration: {Duration}ms){Exception}",
                    entry.Key,
                    status,
                    description,
                    duration,
                    exception != null ? $" Exception: {exception.Message}" : string.Empty);
            }
        }

        logger.LogInformation("Health checks completed");
    }
}



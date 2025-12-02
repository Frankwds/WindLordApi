using Microsoft.Extensions.Diagnostics.HealthChecks;
using WindLordApi.Data;

namespace WindLordApi.Worker.Startup;

/// <summary>
/// Health check for database connectivity.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(
        ApplicationDbContext dbContext,
        ILogger<DatabaseHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple database connectivity check
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (canConnect)
            {
                _logger.LogInformation("Database health check passed");
                return HealthCheckResult.Healthy("Database is accessible");
            }
            else
            {
                _logger.LogWarning("Database health check failed: Cannot connect to database");
                return HealthCheckResult.Unhealthy("Database is not accessible");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed with exception");
            return HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
}


using Microsoft.Extensions.Diagnostics.HealthChecks;
using WindLordApi.Data.Schema;

namespace WindLordApi.Worker.Startup;

public abstract class TableSchemaHealthCheck<TEntity, THealthCheck> : IHealthCheck
    where TEntity : class
    where THealthCheck : class
{
    private readonly TableSchemaValidationService _schemaValidationService;
    private readonly ILogger<THealthCheck> _logger;

    protected TableSchemaHealthCheck(
        TableSchemaValidationService schemaValidationService,
        ILogger<THealthCheck> logger)
    {
        _schemaValidationService = schemaValidationService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = await _schemaValidationService.ValidateAsync<TEntity>(cancellationToken);

            if (validationResult.IsValid)
            {
                _logger.LogInformation(validationResult.Message);
                return HealthCheckResult.Healthy(validationResult.Message);
            }

            _logger.LogError(validationResult.Message);
            return HealthCheckResult.Unhealthy(validationResult.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema contract health check failed with exception");
            return HealthCheckResult.Unhealthy("Schema contract health check failed", ex);
        }
    }
}
using WindLordApi.Data.Models;
using WindLordApi.Data.Schema;

namespace WindLordApi.Worker.Startup;

/// <summary>
/// Health check that validates the forecast_cache table contract used by WindLordApi.
/// </summary>
public sealed class ForecastCacheSchemaHealthCheck
    : TableSchemaHealthCheck<ForecastCache, ForecastCacheSchemaHealthCheck>
{
    public ForecastCacheSchemaHealthCheck(
        TableSchemaValidationService schemaValidationService,
        ILogger<ForecastCacheSchemaHealthCheck> logger)
        : base(schemaValidationService, logger)
    {
    }
}
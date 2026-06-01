using WindLordApi.Data.Models;
using WindLordApi.Data.Schema;

namespace WindLordApi.Worker.Startup;

/// <summary>
/// Health check that validates the station_data table contract used by WindLordApi.
/// </summary>
public sealed class StationDataSchemaHealthCheck
    : TableSchemaHealthCheck<StationData, StationDataSchemaHealthCheck>
{
    public StationDataSchemaHealthCheck(
        TableSchemaValidationService schemaValidationService,
        ILogger<StationDataSchemaHealthCheck> logger)
        : base(schemaValidationService, logger)
    {
    }
}
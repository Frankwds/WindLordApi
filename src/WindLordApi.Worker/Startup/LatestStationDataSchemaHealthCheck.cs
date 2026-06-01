using WindLordApi.Data.Models;
using WindLordApi.Data.Schema;

namespace WindLordApi.Worker.Startup;

/// <summary>
/// Health check that validates the latest_station_data table contract used by WindLordApi.
/// </summary>
public sealed class LatestStationDataSchemaHealthCheck
    : TableSchemaHealthCheck<LatestStationData, LatestStationDataSchemaHealthCheck>
{
    public LatestStationDataSchemaHealthCheck(
        TableSchemaValidationService schemaValidationService,
        ILogger<LatestStationDataSchemaHealthCheck> logger)
        : base(schemaValidationService, logger)
    {
    }
}
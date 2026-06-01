using WindLordApi.Data.Models;
using WindLordApi.Data.Schema;

namespace WindLordApi.Worker.Startup;

/// <summary>
/// Health check that validates the all_paragliding_locations table contract used by WindLordApi.
/// </summary>
public sealed class ParaglidingLocationSchemaHealthCheck
    : TableSchemaHealthCheck<ParaglidingLocation, ParaglidingLocationSchemaHealthCheck>
{
    public ParaglidingLocationSchemaHealthCheck(
        TableSchemaValidationService schemaValidationService,
        ILogger<ParaglidingLocationSchemaHealthCheck> logger)
        : base(schemaValidationService, logger)
    {
    }
}
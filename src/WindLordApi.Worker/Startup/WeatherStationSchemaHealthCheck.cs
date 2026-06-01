using WindLordApi.Data.Models;
using WindLordApi.Data.Schema;

namespace WindLordApi.Worker.Startup;

/// <summary>
/// Health check that validates the weather_stations table contract used by WindLordApi.
/// </summary>
public sealed class WeatherStationSchemaHealthCheck
    : TableSchemaHealthCheck<WeatherStation, WeatherStationSchemaHealthCheck>
{
    public WeatherStationSchemaHealthCheck(
        TableSchemaValidationService schemaValidationService,
        ILogger<WeatherStationSchemaHealthCheck> logger)
        : base(schemaValidationService, logger)
    {
    }
}
using Microsoft.Extensions.Logging;
using WindLordApi.Data.Services;
using WindLordApi.Integrations.WindsMobi;

namespace WindLordApi.Worker.Services;

/// <summary>
/// Service for syncing weather station data from WindsMobi API.
/// </summary>
public class WindsMobiSyncService : IWindsMobiSyncService
{
    private readonly IWindsMobiClient _windsMobiClient;
    private readonly IWeatherStationService _weatherStationService;
    private readonly IStationDataService _stationDataService;
    private readonly ILatestStationDataService _latestStationDataService;
    private readonly ILogger<WindsMobiSyncService> _logger;

    public WindsMobiSyncService(
        IWindsMobiClient windsMobiClient,
        IWeatherStationService weatherStationService,
        IStationDataService stationDataService,
        ILatestStationDataService latestStationDataService,
        ILogger<WindsMobiSyncService> logger)
    {
        _windsMobiClient = windsMobiClient;
        _weatherStationService = weatherStationService;
        _stationDataService = stationDataService;
        _latestStationDataService = latestStationDataService;
        _logger = logger;
    }

    public async Task<int> SyncWindsMobiDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Fetch all data from WindsMobi API (all providers)
            _logger.LogInformation("WindsMobi: Fetching weather stations and station data from all providers...");
            var windsMobiData = await _windsMobiClient.FetchAllProvidersAsync(cancellationToken);

            // 2. Upsert WeatherStations -
            // Do this first in case WindsMobi just added a new station,
            // to make sure there is a weather station to match the station data that follows
            if (windsMobiData.WeatherStations.Count > 0)
            {
                var weatherStationsArray = windsMobiData.WeatherStations.ToArray();
                await _weatherStationService.UpsertManyAsync(weatherStationsArray, cancellationToken);
                _logger.LogInformation("WindsMobi: Upserted {Count} weather station records (Perhaps with no changes)", weatherStationsArray.Length);
            }
            else
            {
                _logger.LogWarning("WindsMobi: No weather stations to upsert");
            }

            // 3. Upsert StationData
            var stationDataInserted = 0;
            if (windsMobiData.StationData.Count > 0)
            {
                var stationDataArray = windsMobiData.StationData.ToArray();
                stationDataInserted = await _stationDataService.UpsertManyAsync(stationDataArray, cancellationToken);
                _logger.LogInformation("WindsMobi: Successfully inserted {Inserted}/{Attempted} new station data records",
                    stationDataInserted, stationDataArray.Length);

                // 4. Convert StationData to LatestStationData and upsert
                var latestStationDataArray = LatestStationDataService.ConvertFromStationData(stationDataArray);
                if (latestStationDataArray.Length > 0)
                {
                    await _latestStationDataService.UpsertManyAsync(latestStationDataArray, cancellationToken);
                    _logger.LogInformation("WindsMobi: Upserted {Count} latest station data records (Perhaps with no changes)", latestStationDataArray.Length);
                }
            }
            else
            {
                _logger.LogWarning("WindsMobi: No station data to upsert");
            }

            _logger.LogInformation("WindsMobi: Completed sync. New station data records inserted: {Inserted}", stationDataInserted);
            return stationDataInserted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WindsMobi: Error syncing data");
            throw;
        }
    }
}

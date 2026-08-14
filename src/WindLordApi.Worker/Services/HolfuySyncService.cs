using Microsoft.Extensions.Logging;
using WindLordApi.Data.Services;
using WindLordApi.Integrations.Holfuy;

namespace WindLordApi.Worker.Services;

/// <summary>
/// Service for syncing weather station data from Holfuy API
/// </summary>
public class HolfuySyncService : IHolfuySyncService
{
    private readonly IHolfuyClient _holfuyClient;
    private readonly IWeatherStationService _weatherStationService;
    private readonly IStationDataService _stationDataService;
    private readonly ILatestStationDataService _latestStationDataService;
    private readonly ILogger<HolfuySyncService> _logger;

    public HolfuySyncService(
        IHolfuyClient holfuyClient,
        IWeatherStationService weatherStationService,
        IStationDataService stationDataService,
        ILatestStationDataService latestStationDataService,
        ILogger<HolfuySyncService> logger)
    {
        _holfuyClient = holfuyClient;
        _weatherStationService = weatherStationService;
        _stationDataService = stationDataService;
        _latestStationDataService = latestStationDataService;
        _logger = logger;
    }

    public async Task<int> SyncHolfuyDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Fetch all data from Holfuy API
            _logger.LogInformation("Holfuy: Fetching weather stations and station data...");
            var holfuyData = await _holfuyClient.FetchHolfuyDataAsync(cancellationToken);

            // 2. Upsert WeatherStations - 
            // Do this first in case holfuy just added a new station, 
            // to make sure there is a weather station to match the station data that follows
            if (holfuyData.WeatherStations.Count > 0)
            {
                var weatherStationsArray = holfuyData.WeatherStations.ToArray();
                await _weatherStationService.UpsertManyAsync(weatherStationsArray, cancellationToken);
                _logger.LogInformation("Holfuy: Upserted {Count} weather station records (Perhaps with no changes)", weatherStationsArray.Length);
            }
            else
            {
                _logger.LogWarning("Holfuy: No weather stations to upsert");
            }

            // 3. Upsert StationData
            var stationDataInserted = 0;
            if (holfuyData.StationData.Count > 0)
            {
                var stationDataArray = holfuyData.StationData.ToArray();
                stationDataInserted = await _stationDataService.UpsertManyAsync(stationDataArray, cancellationToken);
                _logger.LogInformation("Holfuy: Successfully inserted {Inserted}/{Attempted} new station data records",
                    stationDataInserted, stationDataArray.Length);

                // 4. Convert StationData to LatestStationData and upsert
                var latestStationDataArray = LatestStationDataService.ConvertFromStationData(stationDataArray);
                if (latestStationDataArray.Length > 0)
                {
                    await _latestStationDataService.UpsertManyAsync(latestStationDataArray, cancellationToken);
                    _logger.LogInformation("Holfuy: Upserted {Count} latest station data records (Perhaps with no changes)", latestStationDataArray.Length);
                }
            }
            else
            {
                _logger.LogWarning("Holfuy: No station data to upsert");
            }

            _logger.LogInformation("Holfuy: Completed sync. New station data records inserted: {Inserted}", stationDataInserted);
            return stationDataInserted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Holfuy: Error syncing data");
            throw;
        }
    }

    public async Task<int> DeactivateStaleStationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Retention deletes station_data older than 24 hours, so "no data" means the
            // station has been silent for over 24 hours. Stations reappearing in the s=all
            // response are automatically reactivated by the weather-station upsert.
            var deactivatedCount = await _weatherStationService.SetAllStationsWithoutDataToInactiveByProviderAsync("Holfuy", cancellationToken);
            _logger.LogInformation("Holfuy: Deactivated {Count} station(s) with no data in the last 24 hours", deactivatedCount);
            return deactivatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Holfuy: Error deactivating stale stations");
            throw;
        }
    }
}


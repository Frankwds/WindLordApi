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
            _logger.LogInformation("Fetching weather stations and station data from Holfuy API...");
            var holfuyData = await _holfuyClient.FetchHolfuyDataAsync(cancellationToken);

            _logger.LogInformation("Received {StationCount} weather stations and {DataCount} station data records from Holfuy API",
                holfuyData.WeatherStations.Count, holfuyData.StationData.Count);

            var totalUpserted = 0;

            // 2. Upsert WeatherStations
            if (holfuyData.WeatherStations.Count > 0)
            {
                var weatherStationsArray = holfuyData.WeatherStations.ToArray();
                var weatherStationsUpserted = await _weatherStationService.UpsertManyAsync(weatherStationsArray, cancellationToken);
                _logger.LogInformation("Upserted {Upserted}/{Attempted} weather station records",
                    weatherStationsUpserted, weatherStationsArray.Length);
                totalUpserted += weatherStationsUpserted;
            }
            else
            {
                _logger.LogWarning("No weather stations to upsert");
            }

            // 3. Upsert StationData
            if (holfuyData.StationData.Count > 0)
            {
                var stationDataArray = holfuyData.StationData.ToArray();
                var stationDataUpserted = await _stationDataService.UpsertManyAsync(stationDataArray, cancellationToken);
                _logger.LogInformation("Upserted {Upserted}/{Attempted} station data records",
                    stationDataUpserted, stationDataArray.Length);
                totalUpserted += stationDataUpserted;

                // 4. Convert StationData to LatestStationData and upsert
                var latestStationDataArray = LatestStationDataService.ConvertFromStationData(stationDataArray);
                if (latestStationDataArray.Length > 0)
                {
                    var latestStationDataUpserted = await _latestStationDataService.UpsertManyAsync(latestStationDataArray, cancellationToken);
                    _logger.LogInformation("Upserted {Upserted}/{Attempted} latest station data records",
                        latestStationDataUpserted, latestStationDataArray.Length);
                    totalUpserted += latestStationDataUpserted;
                }
            }
            else
            {
                _logger.LogWarning("No station data to upsert");
            }

            _logger.LogInformation("Completed Holfuy sync. Total records upserted: {Total}", totalUpserted);
            return totalUpserted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing data from Holfuy API");
            throw;
        }
    }
}


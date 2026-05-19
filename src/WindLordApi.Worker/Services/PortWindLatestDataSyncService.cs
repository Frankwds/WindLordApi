using WindLordApi.Data.Services;
using WindLordApi.Integrations.PortWind;

namespace WindLordApi.Worker.Services;

/// <summary>
/// Syncs latest PortWind observations for active PortWind stations.
/// </summary>
public class PortWindLatestDataSyncService : IPortWindLatestDataSyncService
{
    private const string Provider = "PortWind";
    private readonly IPortWindClient _portWindClient;
    private readonly IPortWindMapping _portWindMapping;
    private readonly IWeatherStationService _weatherStationService;
    private readonly IStationDataService _stationDataService;
    private readonly ILatestStationDataService _latestStationDataService;
    private readonly ILogger<PortWindLatestDataSyncService> _logger;

    public PortWindLatestDataSyncService(
        IPortWindClient portWindClient,
        IPortWindMapping portWindMapping,
        IWeatherStationService weatherStationService,
        IStationDataService stationDataService,
        ILatestStationDataService latestStationDataService,
        ILogger<PortWindLatestDataSyncService> logger)
    {
        _portWindClient = portWindClient;
        _portWindMapping = portWindMapping;
        _weatherStationService = weatherStationService;
        _stationDataService = stationDataService;
        _latestStationDataService = latestStationDataService;
        _logger = logger;
    }

    public async Task<int> SyncLatestStationDataAsync(CancellationToken cancellationToken = default)
    {
        var stationIds = (await _weatherStationService.GetActiveStationIdsByProviderAsync(Provider, cancellationToken)).ToList();
        if (stationIds.Count == 0)
        {
            _logger.LogWarning("PortWind: No active stations found to sync");
            return 0;
        }

        var totalInserted = 0;
        var totalAttempted = 0;

        foreach (var stationId in stationIds)
        {
            try
            {
                var latestResponse = await _portWindClient.FetchLatestDataAsync(stationId, cancellationToken);
                if (latestResponse is null)
                {
                    continue;
                }

                var stationData = _portWindMapping.MapToStationData(stationId, latestResponse);
                if (stationData is null)
                {
                    _logger.LogDebug("PortWind: No persistable latest data for station {StationId}", stationId);
                    continue;
                }

                var stationDataArray = new[] { stationData };
                totalAttempted += 1;
                totalInserted += await _stationDataService.UpsertManyAsync(stationDataArray, cancellationToken);

                var latestStationDataArray = LatestStationDataService.ConvertFromStationData(stationDataArray);
                if (latestStationDataArray.Length > 0)
                {
                    await _latestStationDataService.UpsertManyAsync(latestStationDataArray, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PortWind: Error syncing latest data for station {StationId}", stationId);
            }
        }

        _logger.LogInformation("PortWind: Inserted {Inserted}/{Attempted} latest station data records", totalInserted, totalAttempted);
        return totalInserted;
    }
}
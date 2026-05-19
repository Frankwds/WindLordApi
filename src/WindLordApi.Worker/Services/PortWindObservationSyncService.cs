using Microsoft.Extensions.Logging;
using WindLordApi.Data.Services;
using WindLordApi.Integrations.PortWind;

namespace WindLordApi.Worker.Services;

public class PortWindObservationSyncService : IPortWindObservationSyncService
{
    private readonly IPortWindClient _portWindClient;
    private readonly IPortWindMapping _portWindMapping;
    private readonly IWeatherStationService _weatherStationService;
    private readonly IStationDataService _stationDataService;
    private readonly ILatestStationDataService _latestStationDataService;
    private readonly ILogger<PortWindObservationSyncService> _logger;

    public PortWindObservationSyncService(
        IPortWindClient portWindClient,
        IPortWindMapping portWindMapping,
        IWeatherStationService weatherStationService,
        IStationDataService stationDataService,
        ILatestStationDataService latestStationDataService,
        ILogger<PortWindObservationSyncService> logger)
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
        var stationIds = (await _weatherStationService
                .GetActiveStationIdsByProviderAsync(PortWindOptions.ProviderName, cancellationToken))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (stationIds.Count == 0)
        {
            _logger.LogWarning("PortWind: No active stations found to sync");
            return 0;
        }

        _logger.LogInformation("PortWind: Syncing observations for {Count} active station(s)", stationIds.Count);

        var totalInserted = 0;

        foreach (var stationId in stationIds)
        {
            try
            {
                var response = await _portWindClient.FetchLatestAndPreviousObservationAsync(stationId, cancellationToken);
                if (response.Data.Count == 0)
                {
                    _logger.LogDebug("PortWind: Station {StationId} returned no observations", stationId);
                    continue;
                }

                var stationData = _portWindMapping.MapObservations(stationId, response.Data);
                if (stationData.Count == 0)
                {
                    _logger.LogDebug("PortWind: Station {StationId} returned no mappable observations", stationId);
                    continue;
                }

                var stationDataArray = stationData.ToArray();
                totalInserted += await _stationDataService.UpsertManyAsync(stationDataArray, cancellationToken);

                var latestStationData = LatestStationDataService.ConvertFromStationData(stationDataArray);
                if (latestStationData.Length > 0)
                {
                    await _latestStationDataService.UpsertManyAsync(latestStationData, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PortWind: Error syncing observations for station {StationId}", stationId);
            }
        }

        _logger.LogInformation("PortWind: Completed observation sync. New station data records inserted: {Inserted}", totalInserted);
        return totalInserted;
    }
}
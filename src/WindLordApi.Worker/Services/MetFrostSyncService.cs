using Microsoft.Extensions.Logging;
using WindLordApi.Data.Services;
using WindLordApi.Integrations.MetFrost;

namespace WindLordApi.Worker.Services;

public class MetFrostSyncService : IMetFrostSyncService
{
    private readonly IWeatherStationService _weatherStationService;
    private readonly IMetFrostClient _metFrostClient;
    private readonly IStationDataService _stationDataService;
    private readonly ILogger<MetFrostSyncService> _logger;
    private const int MaxStationsPerRequest = 100; // Based on MetFrost API limit

    public MetFrostSyncService(
        IWeatherStationService weatherStationService,
        IMetFrostClient metFrostClient,
        IStationDataService stationDataService,
        ILogger<MetFrostSyncService> logger)
    {
        _weatherStationService = weatherStationService;
        _metFrostClient = metFrostClient;
        _stationDataService = stationDataService;
        _logger = logger;
    }

    public async Task<int> SyncAllStationsAsync(CancellationToken cancellationToken = default)
    {
        // 1. Fetch all active station IDs from database
        var stationIds = (await _weatherStationService.GetActiveMETStationIdsAsync(cancellationToken)).ToList();

        if (stationIds.Count == 0)
        {
            _logger.LogWarning("No active stations found to sync");
            return 0;
        }

        _logger.LogInformation("Starting sync for {Count} stations", stationIds.Count);

        var totalUpserted = 0;

        // 2. Process stations in batches (MetFrost API limit is 100)
        for (int i = 0; i < stationIds.Count; i += MaxStationsPerRequest)
        {
            var batch = stationIds.Skip(i).Take(MaxStationsPerRequest).ToArray();

            try
            {
                // 3. Fetch data from MetFrost API
                var response = await _metFrostClient.FetchMetStationDataAsync(batch, cancellationToken);
                _logger.LogInformation("Fetched {Count} data points for batch {BatchNumber}",
                    response.Data.Count, (i / MaxStationsPerRequest) + 1);

                // 4. Map MET observations to StationData
                var stationDataList = MetFrostMapping.MapMetObservationsToStationData(response.Data);
                _logger.LogInformation("Mapped {Count} valid station data records from {DataPointCount} data points",
                    stationDataList.Count, response.Data.Count);

                // 5. Upsert the mapped data to database
                if (stationDataList.Count > 0)
                {
                    await _stationDataService.UpsertManyAsync(stationDataList.ToArray(), cancellationToken);
                    totalUpserted += stationDataList.Count;
                    _logger.LogInformation("Successfully upserted {Count} station data records for batch {BatchNumber}",
                        stationDataList.Count, (i / MaxStationsPerRequest) + 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch {BatchNumber} with stations: {Stations}",
                    (i / MaxStationsPerRequest) + 1, string.Join(", ", batch));
                // Continue with next batch instead of failing completely
            }
        }

        _logger.LogInformation("Sync completed. Total records upserted: {Count}", totalUpserted);
        return totalUpserted;
    }
}


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

        var totalAttempted = 0;
        var totalInserted = 0;
        var batchNumber = 0;

        // 2. Process stations in batches (MetFrost API limit is 100)
        for (int i = 0; i < stationIds.Count; i += MaxStationsPerRequest)
        {
            var batch = stationIds.Skip(i).Take(MaxStationsPerRequest).ToArray();
            batchNumber++;
            var processedCount = Math.Min(i + MaxStationsPerRequest, stationIds.Count);

            _logger.LogInformation("Processing batch {BatchNumber}, {Processed}/{Total} station_ids",
                batchNumber, processedCount, stationIds.Count);

            try
            {
                // 3. Fetch data from MetFrost API
                var response = await _metFrostClient.FetchMetStationDataAsync(batch, cancellationToken);

                // 4. Map MET observations to StationData
                var stationDataList = MetFrostMapping.MapMetObservationsToStationData(response.Data);

                // 5. Upsert the mapped data to database
                if (stationDataList.Count > 0)
                {
                    var inserted = await _stationDataService.UpsertManyAsync(stationDataList.ToArray(), cancellationToken);
                    totalAttempted += stationDataList.Count;
                    totalInserted += inserted;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch {BatchNumber} with stations: {Stations}",
                    batchNumber, string.Join(", ", batch));
                // Continue with next batch instead of failing completely
            }
        }

        _logger.LogInformation("Inserted {Inserted}/{Attempted} new records of station data",
            totalInserted, totalAttempted);
        return totalInserted;
    }
}


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

    public async Task<int> SyncLatestStationDataAsync(CancellationToken cancellationToken = default)
    {
        return await SyncStationDataAsync(isActive: true, cancellationToken);
    }

    private async Task<int> SyncStationDataAsync(bool isActive, CancellationToken cancellationToken = default)
    {
        // 1. Fetch station IDs based on active status
        var stationIds = isActive
            ? (await _weatherStationService.GetActiveMETStationIdsAsync(cancellationToken)).ToList()
            : (await _weatherStationService.GetInactiveMETStationIdsAsync(cancellationToken)).ToList();

        var statusLabel = isActive ? "active" : "inactive";
        if (stationIds.Count == 0)
        {
            _logger.LogWarning("No {Status} stations found to sync", statusLabel);
            return 0;
        }

        _logger.LogInformation("Syncing {Count} {Status} station(s)", stationIds.Count, statusLabel);

        var totalAttempted = 0;
        var totalInserted = 0;
        var batchNumber = 0;

        // 2. Process stations in batches (MetFrost API limit is 100)
        for (int i = 0; i < stationIds.Count; i += MaxStationsPerRequest)
        {
            var batch = stationIds.Skip(i).Take(MaxStationsPerRequest).ToArray();
            batchNumber++;
            var processedCount = Math.Min(i + MaxStationsPerRequest, stationIds.Count);

            _logger.LogInformation("Processing batch {BatchNumber}, {Processed}/{Total} {Status} station_ids",
                batchNumber, processedCount, stationIds.Count, statusLabel);

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
                _logger.LogError(ex, "Error processing batch {BatchNumber} with {Status} stations: {Stations}",
                    batchNumber, statusLabel, string.Join(", ", batch));
                // Continue with next batch instead of failing completely
            }
        }

        _logger.LogInformation("Inserted {Inserted}/{Attempted} new records of station data for {Status} stations",
            totalInserted, totalAttempted, statusLabel);
        return totalInserted;
    }

    public async Task<int> SyncNewWeatherStationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Fetch all stations from MetFrost API
            _logger.LogInformation("Fetching weather stations from MetFrost API...");
            var response = await _metFrostClient.FetchMetFrostStationsAsync(cancellationToken);

            _logger.LogInformation("Received {Count} stations from MetFrost API", response.Data.Count);

            // 2. Map MET stations to WeatherStation format
            var weatherStations = MetFrostMapping.MapMetFrostToWeatherStation(response.Data);

            _logger.LogInformation("Mapped {Count} valid weather stations", weatherStations.Count);

            // 3. Upsert the mapped data to database
            if (weatherStations.Count > 0)
            {
                var inserted = await _weatherStationService.UpsertManyAsync(weatherStations.ToArray(), cancellationToken);
                _logger.LogInformation("Inserted {Inserted}/{Attempted} new weather station records",
                    inserted, weatherStations.Count);
                return inserted;
            }

            _logger.LogWarning("No valid weather stations to insert");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing weather stations from MetFrost API");
            throw;
        }
    }

    public async Task<int> SyncWeatherStationActiveStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting weather station active status sync...");

            // 1. First, sync data for all inactive stations to check if they now have data
            _logger.LogInformation("Syncing station data for inactive stations before status update...");
            await SyncStationDataAsync(isActive: false, cancellationToken);

            // 2. Set stations with data to active
            var activatedCount = await _weatherStationService.SetActiveStationsWithDataAsync(cancellationToken);
            _logger.LogInformation("Activated {Count} stations with data", activatedCount);

            // 3. Set stations without data to inactive
            var deactivatedCount = await _weatherStationService.SetInactiveStationsWithoutDataAsync(cancellationToken);
            _logger.LogInformation("Deactivated {Count} stations without data", deactivatedCount);

            var totalUpdated = activatedCount + deactivatedCount;
            _logger.LogInformation("Completed weather station active status sync. Total updated: {Total}", totalUpdated);

            return totalUpdated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing weather station active status");
            throw;
        }
    }
}

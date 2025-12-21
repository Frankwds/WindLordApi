using WindLordApi.Data.Services;
using WindLordApi.Integrations.MetFrost;

namespace WindLordApi.Worker.Services;

public class MetFrostSyncService : IMetFrostSyncService
{
    private readonly IWeatherStationService _weatherStationService;
    private readonly IMetFrostClient _metFrostClient;
    private readonly IStationDataService _stationDataService;
    private readonly ILatestStationDataService _latestStationDataService;
    private readonly IMetFrostMapping _metFrostMapping;
    private readonly ILogger<MetFrostSyncService> _logger;
    private const int MaxStationsPerRequest = 100; // Based on MetFrost API limit

    public MetFrostSyncService(
        IWeatherStationService weatherStationService,
        IMetFrostClient metFrostClient,
        IStationDataService stationDataService,
        ILatestStationDataService latestStationDataService,
        IMetFrostMapping metFrostMapping,
        ILogger<MetFrostSyncService> logger)
    {
        _weatherStationService = weatherStationService;
        _metFrostClient = metFrostClient;
        _stationDataService = stationDataService;
        _latestStationDataService = latestStationDataService;
        _metFrostMapping = metFrostMapping;
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
            _logger.LogWarning("MetFrost: No {Status} stations found to sync", statusLabel);
            return 0;
        }

        _logger.LogInformation("MetFrost: Syncing {Count} {Status} station(s)", stationIds.Count, statusLabel);

        var totalAttempted = 0;
        var totalInserted = 0;
        var batchNumber = 0;

        // 2. Process stations in batches (MetFrost API limit is 100)
        for (int i = 0; i < stationIds.Count; i += MaxStationsPerRequest)
        {
            var batch = stationIds.Skip(i).Take(MaxStationsPerRequest).ToArray();
            batchNumber++;
            var processedCount = Math.Min(i + MaxStationsPerRequest, stationIds.Count);

            _logger.LogDebug("MetFrost: Processing batch {BatchNumber}, {Processed}/{Total} {Status} station_ids",
                batchNumber, processedCount, stationIds.Count, statusLabel);

            try
            {
                // 3. Fetch data from MetFrost API
                var response = await _metFrostClient.FetchMetStationDataAsync(batch, cancellationToken);

                // 4. Map MET observations to StationData
                var stationDataList = _metFrostMapping.MapMetObservationsToStationData(response.Data);

                // 5. Upsert the mapped data to database
                if (stationDataList.Count > 0)
                {
                    var stationDataArray = stationDataList.ToArray();
                    var inserted = await _stationDataService.UpsertManyAsync(stationDataArray, cancellationToken);
                    totalAttempted += stationDataList.Count;
                    totalInserted += inserted;

                    // 6. Upsert to LatestStationData table
                    var latestStationDataArray = LatestStationDataService.ConvertFromStationData(stationDataArray);
                    if (latestStationDataArray.Length > 0)
                    {
                        await _latestStationDataService.UpsertManyAsync(latestStationDataArray, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MetFrost: Error processing batch {BatchNumber} with {Status} stations: {Stations}",
                    batchNumber, statusLabel, string.Join(", ", batch));
                // Continue with next batch instead of failing completely
            }
        }

        _logger.LogInformation("MetFrost: Inserted {Inserted}/{Attempted} new records of station data for {Status} stations",
            totalInserted, totalAttempted, statusLabel);
        return totalInserted;
    }

    public async Task<int> SyncWeatherStationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Fetch all stations from MetFrost API
            _logger.LogInformation("MetFrost: Fetching weather stations...");
            var response = await _metFrostClient.FetchMetFrostStationsAsync(cancellationToken);

            _logger.LogInformation("MetFrost: Received {Count} stations", response.Data.Count);

            // 2. Map MET stations to WeatherStation format
            var weatherStations = _metFrostMapping.MapMetFrostToWeatherStation(response.Data);

            _logger.LogInformation("MetFrost: Mapped {Count} valid weather stations", weatherStations.Count);

            // 3. Upsert the mapped data to database (always updates, so count is not meaningful)
            if (weatherStations.Count > 0)
            {
                await _weatherStationService.UpsertManyAsync(weatherStations.ToArray(), cancellationToken);
                _logger.LogInformation("MetFrost: Upserted {Count} weather station records", weatherStations.Count);
                return weatherStations.Count;
            }

            _logger.LogWarning("MetFrost: No valid weather stations to upsert");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MetFrost: Error syncing weather stations");
            throw;
        }
    }

    public async Task<int> SyncWeatherStationsActiveStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("MetFrost: Starting weather station active status sync...");

            // 1. First, sync data for all inactive stations to check if they now have data
            var stationDataInserted = await SyncStationDataAsync(isActive: false, cancellationToken);

            // 2. Set stations with data to active
            var activatedCount = await _weatherStationService.SetAllStationsWithDataToActiveAsync(cancellationToken);

            // 3. Set stations without data to inactive
            var deactivatedCount = await _weatherStationService.SetAllStationsWithoutDataToInactiveAsync(cancellationToken);

            _logger.LogInformation("MetFrost: Completed weather station active status sync. New station data inserted: {Inserted}, Status updates: {Activated} activated, {Deactivated} deactivated",
                stationDataInserted, activatedCount, deactivatedCount);

            return stationDataInserted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MetFrost: Error syncing weather station active status");
            throw;
        }
    }
}

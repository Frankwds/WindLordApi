using Microsoft.Extensions.Logging;
using WindLordApi.Data.Services;

namespace WindLordApi.Worker.Services;

/// <summary>
/// Deletes historical station observations past the retention window.
/// </summary>
public class StationDataRetentionService : IStationDataRetentionService
{
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    private readonly IStationDataService _stationDataService;
    private readonly ILogger<StationDataRetentionService> _logger;

    public StationDataRetentionService(
        IStationDataService stationDataService,
        ILogger<StationDataRetentionService> logger)
    {
        _stationDataService = stationDataService;
        _logger = logger;
    }

    public async Task<int> CleanupOldObservationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "StationDataRetention: Deleting station_data rows older than {RetentionHours} hours",
            Retention.TotalHours);

        var deletedCount = await _stationDataService.DeleteOlderThanAsync(Retention, cancellationToken);

        _logger.LogInformation(
            "StationDataRetention: Deleted {DeletedCount} station_data rows older than {RetentionHours} hours",
            deletedCount,
            Retention.TotalHours);

        return deletedCount;
    }
}

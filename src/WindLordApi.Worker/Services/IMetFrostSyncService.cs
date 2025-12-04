namespace WindLordApi.Worker.Services;

/// <summary>
/// Service interface for syncing weather station data from MetFrost API
/// </summary>
public interface IMetFrostSyncService
{
    /// <summary>
    /// Syncs latest station data for active MET stations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of new StationData records inserted (only meaningful metric, as other upserts always update existing records).</returns>
    Task<int> SyncLatestStationDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs weather stations from MetFrost API.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of weather stations affected (inserted or updated).</returns>
    Task<int> SyncWeatherStationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs weather station active status based on data availability.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of new StationData records inserted during inactive station sync.</returns>
    Task<int> SyncWeatherStationsActiveStatusAsync(CancellationToken cancellationToken = default);
}


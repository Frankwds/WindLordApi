namespace WindLordApi.Worker.Services;

/// <summary>
/// Service interface for syncing weather station data from WindsMobi API.
/// </summary>
public interface IWindsMobiSyncService
{
    /// <summary>
    /// Syncs all weather stations and station data from all WindsMobi providers.
    /// Upserts WeatherStations, StationData, and LatestStationData in sequence.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of new StationData records inserted.</returns>
    Task<int> SyncWindsMobiDataAsync(CancellationToken cancellationToken = default);
}

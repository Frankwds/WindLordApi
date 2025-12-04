namespace WindLordApi.Worker.Services;

/// <summary>
/// Service interface for syncing weather station data from Holfuy API
/// </summary>
public interface IHolfuySyncService
{
    /// <summary>
    /// Syncs all weather stations and station data from Holfuy API.
    /// Upserts WeatherStations, StationData, and LatestStationData in sequence.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of new StationData records inserted (only meaningful metric, as other upserts always update existing records).</returns>
    Task<int> SyncHolfuyDataAsync(CancellationToken cancellationToken = default);
}


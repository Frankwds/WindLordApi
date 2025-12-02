namespace WindLordApi.Worker.Services;

public interface IMetFrostSyncService
{
    Task<int> SyncLatestStationDataAsync(CancellationToken cancellationToken = default);
    Task<int> SyncWeatherStationsAsync(CancellationToken cancellationToken = default);
    Task<int> SyncWeatherStationsActiveStatusAsync(CancellationToken cancellationToken = default);
}


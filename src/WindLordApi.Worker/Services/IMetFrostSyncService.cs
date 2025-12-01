namespace WindLordApi.Worker.Services;

public interface IMetFrostSyncService
{
    Task<int> SyncLatestStationDataAsync(CancellationToken cancellationToken = default);
    Task<int> SyncNewWeatherStationsAsync(CancellationToken cancellationToken = default);
    Task<int> SyncWeatherStationActiveStatusAsync(CancellationToken cancellationToken = default);
}


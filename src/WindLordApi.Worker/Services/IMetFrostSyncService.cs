namespace WindLordApi.Worker.Services;

public interface IMetFrostSyncService
{
    Task<int> SyncAllStationsAsync(CancellationToken cancellationToken = default);
    Task<int> SyncNewWeatherStationsAsync(CancellationToken cancellationToken = default);
    Task<int> SyncWeatherStationActiveStatusAsync(CancellationToken cancellationToken = default);
}


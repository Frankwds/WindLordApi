namespace WindLordApi.Worker.Services;

public interface IPortWindStationRefreshService
{
    Task<int> SyncWeatherStationsAsync(CancellationToken cancellationToken = default);
}
namespace WindLordApi.Worker.Services;

public interface IPortWindObservationSyncService
{
    Task<int> SyncLatestStationDataAsync(CancellationToken cancellationToken = default);
}
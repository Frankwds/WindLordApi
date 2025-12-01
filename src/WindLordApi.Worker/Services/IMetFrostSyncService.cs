namespace WindLordApi.Worker.Services;

public interface IMetFrostSyncService
{
    Task<int> SyncAllStationsAsync(CancellationToken cancellationToken = default);
}


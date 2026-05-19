namespace WindLordApi.Worker.Services;

/// <summary>
/// Service interface for syncing latest PortWind observations.
/// </summary>
public interface IPortWindLatestDataSyncService
{
    /// <summary>
    /// Syncs the latest PortWind observation data for active PortWind stations.
    /// </summary>
    Task<int> SyncLatestStationDataAsync(CancellationToken cancellationToken = default);
}
namespace WindLordApi.Worker.Services;

/// <summary>
/// Service interface for refreshing PortWind weather stations.
/// </summary>
public interface IPortWindStationRefreshService
{
    /// <summary>
    /// Refreshes PortWind station metadata and provider-authoritative active state.
    /// </summary>
    Task<int> SyncWeatherStationsAsync(CancellationToken cancellationToken = default);
}
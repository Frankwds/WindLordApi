namespace WindLordApi.Integrations.PortWind;

/// <summary>
/// Client for fetching PortWind station metadata and observations.
/// </summary>
public interface IPortWindClient
{
    /// <summary>
    /// Fetches the PortWind station catalog.
    /// </summary>
    Task<IReadOnlyDictionary<string, PortWindStationCatalogEntry>> FetchStationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the latest observation payload for a single PortWind station.
    /// </summary>
    Task<PortWindLatestResponse?> FetchLatestDataAsync(string stationId, CancellationToken cancellationToken = default);
}
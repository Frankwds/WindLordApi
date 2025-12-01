namespace WindLordApi.Integrations.MetFrost;

public interface IMetFrostClient
{
    /// <summary>
    /// Gets the latest station data from MET Frost API as a strongly-typed response.
    /// </summary>
    /// <param name="stationIds">Array of station IDs to fetch data for (should be &lt;= 100 stations).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deserialized MET observations response.</returns>
    Task<MetObservationsResponse> FetchMetStationDataAsync(string[] stationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all available weather stations from MET Frost API.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Raw JSON response as a string.</returns>
    Task<string> FetchMetFrostStationsAsync(CancellationToken cancellationToken = default);
}
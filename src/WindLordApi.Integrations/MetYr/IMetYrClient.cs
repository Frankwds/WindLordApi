namespace WindLordApi.Integrations.MetYr;

/// <summary>
/// Client interface for fetching weather forecast data from MET.no Locationforecast API (Yr).
/// </summary>
public interface IMetYrClient
{
    /// <summary>
    /// Fetches weather forecast data from Yr API for the specified location.
    /// </summary>
    /// <param name="latitude">Latitude of the location (will be formatted to 4 decimal places).</param>
    /// <param name="longitude">Longitude of the location (will be formatted to 4 decimal places).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deserialized MET.no Locationforecast API response.</returns>
    Task<MetYrResponse> FetchYrDataAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}


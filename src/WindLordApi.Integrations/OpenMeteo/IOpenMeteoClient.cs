namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Client interface for fetching weather forecast data from OpenMeteo forecast API.
/// </summary>
public interface IOpenMeteoClient
{

    /// <summary>
    /// Fetches weather forecast data from OpenMeteo API for multiple locations.
    /// </summary>
    /// <param name="latitude">Array of latitudes (will be formatted to 4 decimal places and comma-separated).</param>
    /// <param name="longitude">Array of longitudes (will be formatted to 4 decimal places and comma-separated).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Array of deserialized OpenMeteo forecast API responses, one for each location.</returns>
    Task<OpenMeteoResponse[]> FetchMeteoDataAsync(
        float[] latitude,
        float[] longitude,
        CancellationToken cancellationToken = default);
}


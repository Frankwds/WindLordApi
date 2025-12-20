namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Client interface for fetching weather forecast data from OpenMeteo forecast API.
/// </summary>
public interface IOpenMeteoClient
{
    /// <summary>
    /// Fetches weather forecast data from OpenMeteo API for the specified location(s).
    /// </summary>
    /// <param name="latitude">Latitude of the location(s) (will be formatted to 4 decimal places).</param>
    /// <param name="longitude">Longitude of the location(s) (will be formatted to 4 decimal places).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deserialized OpenMeteo forecast API response.</returns>
    Task<OpenMeteoResponse> FetchMeteoDataAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches weather forecast data from OpenMeteo API for multiple locations.
    /// </summary>
    /// <param name="latitude">Array of latitudes (will be formatted to 4 decimal places and comma-separated).</param>
    /// <param name="longitude">Array of longitudes (will be formatted to 4 decimal places and comma-separated).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deserialized OpenMeteo forecast API response.</returns>
    Task<OpenMeteoResponse> FetchMeteoDataAsync(
        double[] latitude,
        double[] longitude,
        CancellationToken cancellationToken = default);
}


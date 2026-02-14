namespace WindLordApi.Integrations.GoogleGeocoding;

/// <summary>
/// Client interface for Google Geocoding API reverse geocoding.
/// </summary>
public interface IGoogleGeocodingClient
{
    /// <summary>
    /// Reverse geocodes coordinates to extract the country name.
    /// </summary>
    /// <param name="latitude">The latitude coordinate.</param>
    /// <param name="longitude">The longitude coordinate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The country name (long_name), or null if not found.</returns>
    Task<string?> ReverseGeocodeCountryAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);
}

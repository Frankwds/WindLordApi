namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Client interface for fetching batched forecast data from Open-Meteo.
/// </summary>
public interface IOpenMeteoClient
{
    /// <summary>
    /// Fetches Open-Meteo forecast data for a batch of locations.
    /// </summary>
    Task<IReadOnlyList<OpenMeteoForecastResponse>> FetchForecastAsync(
        IReadOnlyList<OpenMeteoRequestLocation> locations,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default);
}
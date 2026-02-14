using WindLordApi.Data.Models;

namespace WindLordApi.Integrations.WindsMobi;

/// <summary>
/// Client interface for fetching weather station data from WindsMobi API.
/// </summary>
public interface IWindsMobiClient
{
    /// <summary>
    /// Fetches station data from all configured WindsMobi providers.
    /// Iterates through each provider sequentially with a delay between requests.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A record containing both StationData and WeatherStation lists from all providers.</returns>
    Task<WindsMobiDataResult> FetchAllProvidersAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result containing both station data and weather station metadata from WindsMobi API.
/// </summary>
public record WindsMobiDataResult
{
    public required List<StationData> StationData { get; init; }
    public required List<WeatherStation> WeatherStations { get; init; }
}

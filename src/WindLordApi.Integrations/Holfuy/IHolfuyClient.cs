using WindLordApi.Data.Models;

namespace WindLordApi.Integrations.Holfuy;

/// <summary>
/// Client interface for fetching weather station data from Holfuy API
/// </summary>
public interface IHolfuyClient
{
    /// <summary>
    /// Fetches all available station data from Holfuy API.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A record containing both StationData and WeatherStation lists.</returns>
    Task<HolfuyDataResult> FetchHolfuyDataAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result containing both station data and weather station metadata from Holfuy API.
/// </summary>
public record HolfuyDataResult
{
    public required List<StationData> StationData { get; init; }
    public required List<WeatherStation> WeatherStations { get; init; }
}


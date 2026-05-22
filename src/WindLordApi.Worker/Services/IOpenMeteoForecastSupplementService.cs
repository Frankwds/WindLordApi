namespace WindLordApi.Worker.Services;

/// <summary>
/// Service interface for supplementing forecast cache data with Open-Meteo takeoff forecasts.
/// </summary>
public interface IOpenMeteoForecastSupplementService
{
    /// <summary>
    /// Supplements forecast coverage for locations whose Open-Meteo-backed rows are missing or stale.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SupplementForecastsAsync(CancellationToken cancellationToken = default);
}
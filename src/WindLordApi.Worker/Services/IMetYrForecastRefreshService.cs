namespace WindLordApi.Worker.Services;

/// <summary>
/// Service interface for refreshing authoritative MetYr forecast data for paragliding locations.
/// </summary>
public interface IMetYrForecastRefreshService
{
    /// <summary>
    /// Updates forecasts for locations with oldest or missing forecast data.
    /// Cleans up old forecasts before processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateForecastsAsync(CancellationToken cancellationToken = default);
}
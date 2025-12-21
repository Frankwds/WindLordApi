namespace WindLordApi.Worker.Services;

/// <summary>
/// Service interface for updating forecast data for paragliding locations.
/// Combines data from OpenMeteo and MetYr APIs.
/// </summary>
public interface IForecastUpdateService
{
    /// <summary>
    /// Updates forecasts for locations with oldest or missing forecast data.
    /// Cleans up old forecasts before processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateForecastsAsync(CancellationToken cancellationToken = default);
}


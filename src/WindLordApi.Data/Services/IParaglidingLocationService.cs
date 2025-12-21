using WindLordApi.Data.Models;

namespace WindLordApi.Data.Services;

/// <summary>
/// Service interface for ParaglidingLocation entity operations.
/// </summary>
public interface IParaglidingLocationService
{
    /// <summary>
    /// Gets active main locations by their IDs.
    /// </summary>
    /// <param name="ids">Collection of location IDs to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of paragliding locations.</returns>
    Task<IEnumerable<ParaglidingLocation>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets locations with their oldest forecast update time (from view).
    /// Returns only main locations that have forecasts, ordered by oldest update time.
    /// </summary>
    /// <param name="limit">Maximum number of locations to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IEnumerable<LocationsWithOldestForecast>> GetLocationsWithOldestForecastAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active main locations that don't have any forecasts yet (from view).
    /// </summary>
    /// <param name="limit">Maximum number of locations to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IEnumerable<LocationsWithoutForecast>> GetLocationsWithoutForecastAsync(int limit, CancellationToken cancellationToken = default);
}


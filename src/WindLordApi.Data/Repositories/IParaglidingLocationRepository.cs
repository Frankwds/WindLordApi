using WindLordApi.Data.Models;

namespace WindLordApi.Data.Repositories;

/// <summary>
/// Repository interface for ParaglidingLocation entity with specific query methods.
/// </summary>
public interface IParaglidingLocationRepository : IRepository<ParaglidingLocation>
{
    /// <summary>
    /// Gets locations with their oldest forecast update time (from view).
    /// Returns only main locations that have forecasts, ordered by oldest update time.
    /// </summary>
    Task<IEnumerable<LocationsWithOldestForecast>> GetLocationsWithOldestForecastAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active main locations that don't have any forecasts yet (from view).
    /// </summary>
    Task<IEnumerable<LocationsWithoutForecast>> GetLocationsWithoutForecastAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active main locations by their IDs.
    /// </summary>
    /// <param name="ids">Collection of location IDs to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of paragliding locations.</returns>
    Task<IEnumerable<ParaglidingLocation>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}


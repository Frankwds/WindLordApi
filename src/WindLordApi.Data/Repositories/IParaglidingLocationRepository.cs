using WindLordApi.Data.Models;

namespace WindLordApi.Data.Repositories;

/// <summary>
/// Repository interface for ParaglidingLocation entity with specific query methods.
/// </summary>
public interface IParaglidingLocationRepository : IRepository<ParaglidingLocation>
{
    /// <summary>
    /// Gets active main locations prioritized for authoritative MetYr refresh.
    /// Locations without forecasts are returned first, then locations whose forecast rows have the oldest update time.
    /// </summary>
    /// <param name="limit">Maximum number of locations to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IEnumerable<Guid>> GetMetYrRefreshCandidatesAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active main locations with the shortest Open-Meteo forecast tail.
    /// Locations without Open-Meteo-backed forecasts are returned first, then locations whose latest Open-Meteo-backed forecast time is earliest.
    /// </summary>
    /// <param name="limit">Maximum number of locations to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IEnumerable<Guid>> GetOpenMeteoRefreshCandidatesAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active main locations by their IDs.
    /// </summary>
    /// <param name="ids">Collection of location IDs to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of paragliding locations.</returns>
    Task<IEnumerable<ParaglidingLocation>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}


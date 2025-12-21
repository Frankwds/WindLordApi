using WindLordApi.Data.Models;

namespace WindLordApi.Data.Services;

/// <summary>
/// Service interface for ForecastCache entity operations.
/// </summary>
public interface IForecastCacheService
{

    /// <summary>
    /// Upserts multiple forecasts with batching.
    /// </summary>
    Task<int> UpsertManyAsync(ForecastCache[] forecasts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes forecasts older than the specified cutoff time.
    /// </summary>
    Task<int> DeleteOldForecastsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default);

}


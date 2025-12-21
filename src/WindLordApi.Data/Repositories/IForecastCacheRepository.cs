using WindLordApi.Data.Models;

namespace WindLordApi.Data.Repositories;

/// <summary>
/// Repository interface for ForecastCache entity with specific query methods.
/// </summary>
public interface IForecastCacheRepository : IRepository<ForecastCache>
{

    /// <summary>
    /// Upserts a range of forecasts using FlexLabs upsert.
    /// </summary>
    Task<int> UpsertRangeAsync(IEnumerable<ForecastCache> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes forecasts older than the specified cutoff time.
    /// </summary>
    Task<int> DeleteOldForecastsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default);
}


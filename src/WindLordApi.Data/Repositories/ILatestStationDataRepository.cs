using WindLordApi.Data.Models;

namespace WindLordApi.Data.Repositories;

/// <summary>
/// Repository interface for LatestStationData entity.
/// </summary>
public interface ILatestStationDataRepository : IRepository<LatestStationData>
{
    /// <summary>
    /// Upserts a range of latest station data using FlexLabs upsert.
    /// </summary>
    Task<int> UpsertRangeAsync(IEnumerable<LatestStationData> entities, CancellationToken cancellationToken = default);
}


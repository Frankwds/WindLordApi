using WindLordApi.Data.Models;

namespace WindLordApi.Data.Repositories;

/// <summary>
/// Repository interface for StationData entity.
/// </summary>
public interface IStationDataRepository : IRepository<StationData>
{
    /// <summary>
    /// Upserts a range of station data using FlexLabs upsert.
    /// </summary>
    Task<int> UpsertRangeAsync(IEnumerable<StationData> entities, CancellationToken cancellationToken = default);
}


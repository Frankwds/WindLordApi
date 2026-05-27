using WindLordApi.Data.Models;

namespace WindLordApi.Data.Services;

public interface IStationDataService
{
    Task<int> UpsertManyAsync(StationData[] stationDataArray, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes station observations older than the supplied retention window (UTC).
    /// </summary>
    Task<int> DeleteOlderThanAsync(TimeSpan retention, CancellationToken cancellationToken = default);
}

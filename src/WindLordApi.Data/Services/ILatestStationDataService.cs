using WindLordApi.Data.Models;

namespace WindLordApi.Data.Services;

public interface ILatestStationDataService
{
    Task<int> UpsertManyAsync(LatestStationData[] latestStationDataArray, CancellationToken cancellationToken = default);
}


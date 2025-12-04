using WindLordApi.Data.Models;

namespace WindLordApi.Data.Services;

public interface IStationDataService
{
    Task<int> UpsertManyAsync(StationData[] stationDataArray, CancellationToken cancellationToken = default);
}

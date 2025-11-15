using WindLordApi.Data.Models;

namespace WindLordApi.Data.Services;

public interface IStationDataService
{
    Task<IEnumerable<StationData>> GetByStationIdAsync(string stationId, CancellationToken cancellationToken = default);
}

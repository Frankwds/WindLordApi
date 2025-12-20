using WindLordApi.Data.Models;

namespace WindLordApi.Data.Services;

public interface IWeatherStationService
{
    Task<IEnumerable<string>> GetActiveMETStationIdsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetInactiveMETStationIdsAsync(CancellationToken cancellationToken = default);
    Task<int> UpsertManyAsync(WeatherStation[] weatherStations, CancellationToken cancellationToken = default);
    Task<int> SetAllStationsWithDataToActiveAsync(CancellationToken cancellationToken = default);
    Task<int> SetAllStationsWithoutDataToInactiveAsync(CancellationToken cancellationToken = default);
}


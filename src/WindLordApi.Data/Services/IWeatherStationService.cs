using WindLordApi.Data.Models;

namespace WindLordApi.Data.Services;

public interface IWeatherStationService
{
    Task<IEnumerable<string>> GetActiveMETStationIdsAsync(CancellationToken cancellationToken = default);
    Task<int> UpsertManyAsync(WeatherStation[] weatherStations, CancellationToken cancellationToken = default);
}


using WindLordApi.Data.Models;

namespace WindLordApi.Data.Services;

public interface IWeatherStationService
{
    Task<IEnumerable<string>> GetStationIdsByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetActiveStationIdsByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetInactiveStationIdsByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task<List<WeatherStation>> GetStationsByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetActiveMETStationIdsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetInactiveMETStationIdsAsync(CancellationToken cancellationToken = default);
    Task<int> UpsertManyAsync(WeatherStation[] weatherStations, CancellationToken cancellationToken = default);
    Task<int> SetAllStationsWithDataToActiveAsync(CancellationToken cancellationToken = default);
    Task<int> SetAllStationsWithoutDataToInactiveAsync(CancellationToken cancellationToken = default);
    Task<int> SetStationsActiveByProviderAsync(string provider, IEnumerable<string> stationIds, CancellationToken cancellationToken = default);
    Task<int> SetStationsInactiveByProviderExceptAsync(string provider, IEnumerable<string> stationIds, CancellationToken cancellationToken = default);
    Task<List<Models.WeatherStation>> GetStationsWithMissingCountryAsync(CancellationToken cancellationToken = default);
    Task<int> UpdateCountriesAsync(WeatherStation[] weatherStations, CancellationToken cancellationToken = default);
}


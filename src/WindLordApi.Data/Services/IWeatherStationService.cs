using WindLordApi.Data.Models;

namespace WindLordApi.Data.Services;

public interface IWeatherStationService
{
    Task<IEnumerable<string>> GetActiveStationIdsByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetInactiveStationIdsByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task<int> UpsertManyAsync(WeatherStation[] weatherStations, CancellationToken cancellationToken = default);
    Task<int> SetAllStationsWithDataToActiveByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task<int> SetAllStationsWithoutDataToInactiveByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task<int> SetStationsActiveByProviderAsync(string provider, IEnumerable<string> stationIds, CancellationToken cancellationToken = default);
    Task<int> SetStationsInactiveByProviderAsync(string provider, IEnumerable<string> stationIds, CancellationToken cancellationToken = default);
    Task<int> SetMissingStationsInactiveByProviderAsync(string provider, IEnumerable<string> seenStationIds, CancellationToken cancellationToken = default);
    Task<List<Models.WeatherStation>> GetStationsWithMissingCountryAsync(CancellationToken cancellationToken = default);
    Task<int> UpdateCountriesAsync(WeatherStation[] weatherStations, CancellationToken cancellationToken = default);
}


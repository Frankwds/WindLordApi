using WindLordApi.Data.Models;

namespace WindLordApi.Data.Repositories;

/// <summary>
/// Repository interface for WeatherStation entity with specific query methods.
/// </summary>
public interface IWeatherStationRepository : IRepository<WeatherStation>
{
    /// <summary>
    /// Gets all active station IDs for the given provider.
    /// </summary>
    Task<IEnumerable<string>> GetActiveStationIdsByProviderAsync(string provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all inactive station IDs for the given provider.
    /// </summary>
    Task<IEnumerable<string>> GetInactiveStationIdsByProviderAsync(string provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets inactive stations with persisted data to active for the given provider.
    /// </summary>
    Task<int> SetAllStationsWithDataToActiveByProviderAsync(string provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets active stations without persisted data to inactive for the given provider.
    /// </summary>
    Task<int> SetAllStationsWithoutDataToInactiveByProviderAsync(string provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the provided station IDs to active for the given provider.
    /// </summary>
    Task<int> SetStationsActiveByProviderAsync(string provider, IEnumerable<string> stationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the provided station IDs to inactive for the given provider.
    /// </summary>
    Task<int> SetStationsInactiveByProviderAsync(string provider, IEnumerable<string> stationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets provider stations missing from the latest maintenance payload to inactive.
    /// </summary>
    Task<int> SetMissingStationsInactiveByProviderAsync(string provider, IEnumerable<string> seenStationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a range of weather stations using FlexLabs upsert.
    /// </summary>
    Task<int> UpsertRangeAsync(IEnumerable<WeatherStation> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all weather stations where Country is null or "UKJENT".
    /// </summary>
    Task<List<WeatherStation>> GetStationsWithMissingCountryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates only the Country and IsMain fields for the given weather stations.
    /// Used by the CountryLocatorService to persist geocoded countries without affecting other fields.
    /// </summary>
    Task<int> UpdateCountriesAsync(IEnumerable<WeatherStation> entities, CancellationToken cancellationToken = default);
}


using WindLordApi.Data.Models;

namespace WindLordApi.Data.Repositories;

/// <summary>
/// Repository interface for WeatherStation entity with specific query methods.
/// </summary>
public interface IWeatherStationRepository : IRepository<WeatherStation>
{
    /// <summary>
    /// Gets all active MET station IDs.
    /// </summary>
    Task<IEnumerable<string>> GetActiveMETStationIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all inactive MET station IDs.
    /// </summary>
    Task<IEnumerable<string>> GetInactiveMETStationIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets stations with data to active status (raw SQL).
    /// </summary>
    Task<int> SetAllStationsWithDataToActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets stations without data to inactive status (raw SQL).
    /// </summary>
    Task<int> SetAllStationsWithoutDataToInactiveAsync(CancellationToken cancellationToken = default);

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


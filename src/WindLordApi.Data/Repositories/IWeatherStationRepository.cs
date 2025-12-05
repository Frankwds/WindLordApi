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
    Task<int> SetActiveStationsWithDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets stations without data to inactive status (raw SQL).
    /// </summary>
    Task<int> SetInactiveStationsWithoutDataAsync(CancellationToken cancellationToken = default);
}


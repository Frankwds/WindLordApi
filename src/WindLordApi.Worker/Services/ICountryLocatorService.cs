namespace WindLordApi.Worker.Services;

/// <summary>
/// Service interface for locating countries of weather stations using reverse geocoding.
/// </summary>
public interface ICountryLocatorService
{
    /// <summary>
    /// Locates and updates the country for all weather stations where Country is null or "UKJENT".
    /// Sets IsMain = true for stations located in Norway.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of stations whose country was successfully located and updated.</returns>
    Task<int> LocateCountriesAsync(CancellationToken cancellationToken = default);
}

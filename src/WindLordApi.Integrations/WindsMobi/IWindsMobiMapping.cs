using WindLordApi.Data.Models;

namespace WindLordApi.Integrations.WindsMobi;

/// <summary>
/// Maps WindsMobi API response data to domain models for database storage.
/// </summary>
public interface IWindsMobiMapping
{
    /// <summary>
    /// Maps WindsMobi station data to StationData format for database storage.
    /// </summary>
    /// <param name="stations">List of WindsMobi station data.</param>
    /// <returns>List of StationData objects ready for database insertion.</returns>
    List<StationData> MapToStationData(IReadOnlyList<WindsMobiStation> stations);

    /// <summary>
    /// Maps WindsMobi station data to WeatherStation format for database storage.
    /// </summary>
    /// <param name="stations">List of WindsMobi station data.</param>
    /// <returns>List of WeatherStation objects ready for database insertion.</returns>
    List<WeatherStation> MapToWeatherStation(IReadOnlyList<WindsMobiStation> stations);
}

using WindLordApi.Data.Models;

namespace WindLordApi.Integrations.Holfuy;

/// <summary>
/// Maps Holfuy API response data to domain models for database storage.
/// </summary>
public interface IHolfuyMapping
{
    /// <summary>
    /// Maps Holfuy API response data to StationData format for database storage.
    /// </summary>
    /// <param name="holfuyData">Array of Holfuy station data</param>
    /// <returns>Array of StationData objects (without Id) ready for database insertion</returns>
    List<StationData> MapHolfuyToStationData(IReadOnlyList<HolfuyStationData> holfuyData);

    /// <summary>
    /// Maps Holfuy API response data to WeatherStation format for database storage.
    /// </summary>
    /// <param name="holfuyData">Array of Holfuy station data</param>
    /// <returns>Array of WeatherStation objects (without Id and UpdatedAt) ready for database insertion</returns>
    List<WeatherStation> MapHolfuyToWeatherStation(IReadOnlyList<HolfuyStationData> holfuyData);
}


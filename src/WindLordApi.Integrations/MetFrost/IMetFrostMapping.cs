using WindLordApi.Data.Models;

namespace WindLordApi.Integrations.MetFrost;

/// <summary>
/// Maps MET observations data to domain models for database storage.
/// </summary>
public interface IMetFrostMapping
{
    /// <summary>
    /// Maps MET observations data to StationData format for database storage.
    /// Groups observations by station and time, extracting the latest values for each parameter.
    /// </summary>
    /// <param name="observationsData">Array of MET observations data points</param>
    /// <returns>Array of StationData objects (without Id) ready for database insertion</returns>
    List<StationData> MapMetObservationsToStationData(IReadOnlyList<MetObservationsData> observationsData);

    /// <summary>
    /// Maps Met Frost API response data to WeatherStation format for database storage.
    /// </summary>
    /// <param name="metFrostData">Array of MET Frost station data</param>
    /// <returns>Array of WeatherStation objects (without Id and UpdatedAt) ready for database insertion</returns>
    List<WeatherStation> MapMetFrostToWeatherStation(IReadOnlyList<MetFrostStation> metFrostData);
}


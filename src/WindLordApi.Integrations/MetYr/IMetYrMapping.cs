namespace WindLordApi.Integrations.MetYr;

/// <summary>
/// Maps MET.no Locationforecast API response data to DTOs.
/// </summary>
public interface IMetYrMapping
{
    /// <summary>
    /// Maps raw API response to WeatherDataYr DTO.
    /// Implements the mapYrData logic from Next.js.
    /// </summary>
    /// <param name="rawData">Raw API response from MET.no Locationforecast API.</param>
    /// <returns>WeatherDataYr DTO with hourly and six-hourly forecast data.</returns>
    WeatherDataYr MapYrData(MetYrResponse rawData);
}


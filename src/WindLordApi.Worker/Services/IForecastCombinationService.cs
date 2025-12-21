using WindLordApi.Data.Models;
using WindLordApi.Integrations.MetYr;
using WindLordApi.Integrations.OpenMeteo;

namespace WindLordApi.Worker.Services;

/// <summary>
/// Service interface for combining weather forecast data from OpenMeteo and MetYr APIs.
/// </summary>
public interface IForecastCombinationService
{
    /// <summary>
    /// Combines hourly weather data from OpenMeteo and MetYr APIs into a unified ForecastCache structure.
    /// </summary>
    /// <param name="meteoData">Hourly weather data points from OpenMeteo API.</param>
    /// <param name="yrData">Hourly weather data points from MetYr API.</param>
    /// <param name="locationId">Location ID.</param>
    /// <returns>Combined hourly forecast data points.</returns>
    IReadOnlyList<ForecastCache> CombineDataSources(
        IReadOnlyList<OpenMeteoDto> meteoData,
        IReadOnlyList<MetYrDto> yrData,
        Guid locationId);
}


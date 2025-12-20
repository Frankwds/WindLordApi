namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Maps OpenMeteo forecast API response data to DTOs.
/// </summary>
public interface IOpenMeteoMapping
{
    /// <summary>
    /// Maps raw API response to WeatherDataPoint array.
    /// Implements the mapOpenMeteoData logic from Next.js.
    /// </summary>
    /// <param name="validatedData">Raw API response from OpenMeteo forecast API.</param>
    /// <returns>Array of WeatherDataPoint DTOs with hourly forecast data.</returns>
    IReadOnlyList<OpenMeteoDto> MapOpenMeteoData(OpenMeteoResponse validatedData);

    /// <summary>
    /// Maps WMO weather code to Yr weather code string.
    /// </summary>
    /// <param name="wmoCode">WMO weather code.</param>
    /// <param name="isDay">Whether it is day (1) or night (0).</param>
    /// <returns>Yr weather code string.</returns>
    string MapWmoToYrWeatherCode(int wmoCode, int isDay);
}


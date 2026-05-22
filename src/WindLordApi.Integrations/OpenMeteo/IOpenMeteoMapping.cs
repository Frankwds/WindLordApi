namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Maps Open-Meteo DTOs into integration models used by the worker.
/// </summary>
public interface IOpenMeteoMapping
{
    /// <summary>
    /// Maps the raw Open-Meteo response blocks into per-location forecast models.
    /// </summary>
    IReadOnlyList<OpenMeteoLocationForecast> MapForecasts(IReadOnlyList<OpenMeteoForecastResponse> responses);
}
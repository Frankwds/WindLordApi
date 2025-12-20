using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Client for fetching weather forecast data from OpenMeteo forecast API.
/// </summary>
public class OpenMeteoClient : IOpenMeteoClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenMeteoOptions _options;
    private readonly ILogger<OpenMeteoClient> _logger;

    private const string DefaultBaseUrl = "https://api.open-meteo.com/v1/forecast";

    public OpenMeteoClient(
        HttpClient httpClient,
        IOptions<OpenMeteoOptions> options,
        ILogger<OpenMeteoClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Fetches weather forecast data from OpenMeteo API for the specified location.
    /// </summary>
    /// <param name="latitude">Latitude of the location (will be formatted to 4 decimal places).</param>
    /// <param name="longitude">Longitude of the location (will be formatted to 4 decimal places).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deserialized OpenMeteo forecast API response.</returns>
    public async Task<OpenMeteoResponse> FetchMeteoDataAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        return await FetchMeteoDataInternalAsync(
            latitude.ToString("F4"),
            longitude.ToString("F4"),
            cancellationToken);
    }

    /// <summary>
    /// Fetches weather forecast data from OpenMeteo API for multiple locations.
    /// </summary>
    /// <param name="latitude">Array of latitudes (will be formatted to 4 decimal places and comma-separated).</param>
    /// <param name="longitude">Array of longitudes (will be formatted to 4 decimal places and comma-separated).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deserialized OpenMeteo forecast API response.</returns>
    public async Task<OpenMeteoResponse> FetchMeteoDataAsync(
        double[] latitude,
        double[] longitude,
        CancellationToken cancellationToken = default)
    {
        var latString = string.Join(",", latitude.Select(lat => lat.ToString("F4")));
        var lonString = string.Join(",", longitude.Select(lon => lon.ToString("F4")));

        return await FetchMeteoDataInternalAsync(latString, lonString, cancellationToken);
    }

    private async Task<OpenMeteoResponse> FetchMeteoDataInternalAsync(
        string latitudeString,
        string longitudeString,
        CancellationToken cancellationToken)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? DefaultBaseUrl
            : _options.BaseUrl;

        var url = new UriBuilder(baseUrl);
        var queryParams = new List<string>
        {
            $"latitude={Uri.EscapeDataString(latitudeString)}",
            $"longitude={Uri.EscapeDataString(longitudeString)}",
            $"wind_speed_unit={Uri.EscapeDataString(_options.WindSpeedUnit)}",
            $"hourly={Uri.EscapeDataString(string.Join(",", _options.HourlyParameters))}",
            $"forecast_days={Uri.EscapeDataString(_options.ForecastDays)}",
            $"models={Uri.EscapeDataString(_options.Models)}",
            $"timezone={Uri.EscapeDataString(_options.Timezone)}"
        };

        url.Query = string.Join("&", queryParams);

        using var request = new HttpRequestMessage(HttpMethod.Get, url.ToString());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("User-Agent", "WindLordApi/1.0");
        request.Headers.Add("Cache-Control", "public, max-age=300, must-revalidate");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "OpenMeteo: Returned error status {StatusCode} for location {Latitude},{Longitude}. Response body: {ErrorContent}",
                    response.StatusCode,
                    latitudeString,
                    longitudeString,
                    errorContent);
                throw new HttpRequestException(
                    $"OpenMeteo forecast API returned error status {response.StatusCode} for location {latitudeString},{longitudeString}. Response body: {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<OpenMeteoResponse>(content);
            if (result is null)
            {
                _logger.LogError("OpenMeteo: Failed to deserialize response to OpenMeteoResponse for location {Latitude},{Longitude}", latitudeString, longitudeString);
                throw new JsonException("Failed to deserialize OpenMeteo forecast API response to OpenMeteoResponse.");
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OpenMeteo: HTTP error while fetching forecast data for {Latitude},{Longitude}: {ErrorMessage}", latitudeString, longitudeString, ex.Message);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "OpenMeteo: Invalid JSON response received for location {Latitude},{Longitude}", latitudeString, longitudeString);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenMeteo: Unexpected error while fetching forecast data for {Latitude},{Longitude}", latitudeString, longitudeString);
            throw;
        }
    }
}


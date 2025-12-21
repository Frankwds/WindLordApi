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
    /// Fetches weather forecast data from OpenMeteo API for multiple locations.
    /// </summary>
    /// <param name="latitude">Array of latitudes (will be formatted to 4 decimal places and comma-separated).</param>
    /// <param name="longitude">Array of longitudes (will be formatted to 4 decimal places and comma-separated).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Array of deserialized OpenMeteo forecast API responses, one for each location.</returns>
    public async Task<OpenMeteoResponse[]> FetchMeteoDataAsync(
        float[] latitude,
        float[] longitude,
        CancellationToken cancellationToken = default)
    {

        if (latitude.Length != longitude.Length || latitude.Length == 0)
        {
            throw new ArgumentException("Latitude and longitude arrays must be of the same length and not empty.");
        }

        var latStringCsv = string.Join(",", latitude.Select(lat => lat.ToString("F4")));
        var lonStringCsv = string.Join(",", longitude.Select(lon => lon.ToString("F4")));

        string content = string.Empty;
        try
        {
            content = await FetchMeteoDataContentAsync(latStringCsv, lonStringCsv, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenMeteo: Unexpected error while fetching forecast data for {Latitude},{Longitude}", latStringCsv, lonStringCsv);
            throw;
        }


        try
        {
            if (latitude.Length == 1)
            {
                var result = JsonSerializer.Deserialize<OpenMeteoResponse>(content);
                if (result is null)
                {
                    _logger.LogError("OpenMeteo: Failed to deserialize response to OpenMeteoResponse for location {Latitude},{Longitude}", latStringCsv, lonStringCsv);
                    throw new JsonException("Failed to deserialize OpenMeteo forecast API response to OpenMeteoResponse.");
                }
                return [result];
            }
            else
            {
                var result = JsonSerializer.Deserialize<OpenMeteoResponse[]>(content);
                if (result is null)
                {
                    _logger.LogError("OpenMeteo: Failed to deserialize response to OpenMeteoResponse[] for locations {Latitude},{Longitude}", latStringCsv, lonStringCsv);
                    throw new JsonException("Failed to deserialize OpenMeteo forecast API response to OpenMeteoResponse[].");
                }
                return result;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "OpenMeteo: Invalid JSON response received for locations {Latitude},{Longitude}", latStringCsv, lonStringCsv);
            throw;
        }
    }

    private async Task<string> FetchMeteoDataContentAsync(
        string latStringCsv,
        string lonStringCsv,
        CancellationToken cancellationToken)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? DefaultBaseUrl
            : _options.BaseUrl;

        var url = new UriBuilder(baseUrl);
        var queryParams = new List<string>
        {
            $"latitude={Uri.EscapeDataString(latStringCsv)}",
            $"longitude={Uri.EscapeDataString(lonStringCsv)}",
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
                    "OpenMeteo: Returned error status {StatusCode} for location(s) {Latitude},{Longitude}. Response body: {ErrorContent}",
                    response.StatusCode,
                    latStringCsv,
                    lonStringCsv,
                    errorContent);
                throw new HttpRequestException(
                    $"OpenMeteo forecast API returned error status {response.StatusCode} for location(s) {latStringCsv},{lonStringCsv}. Response body: {errorContent}");
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OpenMeteo: HTTP error while fetching forecast data for {Latitude},{Longitude}: {ErrorMessage}", latStringCsv, lonStringCsv, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenMeteo: Unexpected error while fetching forecast data for {Latitude},{Longitude}", latStringCsv, lonStringCsv);
            throw;
        }
    }
}


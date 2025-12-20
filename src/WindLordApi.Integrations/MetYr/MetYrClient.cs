using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WindLordApi.Integrations.MetYr;

/// <summary>
/// Client for fetching weather forecast data from MET.no Locationforecast API (Yr).
/// </summary>
public class MetYrClient : IMetYrClient
{
    private readonly HttpClient _httpClient;
    private readonly MetYrOptions _options;
    private readonly ILogger<MetYrClient> _logger;

    private const string DefaultBaseUrl = "https://api.met.no/weatherapi/locationforecast/2.0/complete";

    public MetYrClient(
        HttpClient httpClient,
        IOptions<MetYrOptions> options,
        ILogger<MetYrClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Fetches weather forecast data from Yr API for the specified location.
    /// </summary>
    /// <param name="latitude">Latitude of the location (will be formatted to 4 decimal places).</param>
    /// <param name="longitude">Longitude of the location (will be formatted to 4 decimal places).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deserialized MET.no Locationforecast API response.</returns>
    public async Task<MetYrResponse> FetchYrDataAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? DefaultBaseUrl
            : _options.BaseUrl;

        var url = $"{baseUrl}?lat={latitude:F4}&lon={longitude:F4}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("User-Agent", "windlord (https://windalert.vercel.app/)");
        request.Headers.Add("Cache-Control", "public, max-age=300, must-revalidate");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "MetYr: Returned error status {StatusCode} for location {Latitude},{Longitude}. Response body: {ErrorContent}",
                    response.StatusCode,
                    latitude,
                    longitude,
                    errorContent);
                throw new HttpRequestException(
                    $"MET.no Locationforecast API returned error status {response.StatusCode} for location {latitude},{longitude}. Response body: {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<MetYrResponse>(content);
            if (result is null)
            {
                _logger.LogError("MetYr: Failed to deserialize response to MetYrResponse for location {Latitude},{Longitude}", latitude, longitude);
                throw new JsonException("Failed to deserialize MET.no Locationforecast API response to MetYrResponse.");
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "MetYr: HTTP error while fetching forecast data for {Latitude},{Longitude}: {ErrorMessage}", latitude, longitude, ex.Message);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "MetYr: Invalid JSON response received for location {Latitude},{Longitude}", latitude, longitude);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MetYr: Unexpected error while fetching forecast data for {Latitude},{Longitude}", latitude, longitude);
            throw;
        }
    }
}


using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WindLordApi.Data.Models;

namespace WindLordApi.Integrations.Holfuy;

/// <summary>
/// Client for fetching weather station data from Holfuy API
/// </summary>
public class HolfuyClient : IHolfuyClient
{
    private readonly HttpClient _httpClient;
    private readonly HolfuyOptions _options;
    private readonly IHolfuyMapping _holfuyMapping;
    private readonly ILogger<HolfuyClient> _logger;

    private const string BaseUrl = "https://api.holfuy.com/live/";

    public HolfuyClient(
        HttpClient httpClient,
        IOptions<HolfuyOptions> options,
        IHolfuyMapping holfuyMapping,
        ILogger<HolfuyClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _holfuyMapping = holfuyMapping;
        _logger = logger;
    }

    /// <summary>
    /// Fetches all available station data from Holfuy API.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A record containing both StationData and WeatherStation lists.</returns>
    public async Task<HolfuyDataResult> FetchHolfuyDataAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogError("Holfuy: ApiKey is not configured");
            throw new InvalidOperationException("Holfuy ApiKey is not configured");
        }

        // Build API URL
        var queryParams = new List<string>
        {
            "s=all",
            $"pw={Uri.EscapeDataString(_options.ApiKey)}",
            "m=JSON",
            "tu=C",
            "su=m/s",
            "avg=1",
            "utc",
            "loc"
        };

        var queryString = string.Join("&", queryParams);
        var requestUrl = $"{BaseUrl}?{queryString}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("User-Agent", "WindLordApi/1.0");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Holfuy: Returned error status {StatusCode}. Response body: {ErrorContent}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Holfuy API returned error status {response.StatusCode}. Response body: {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var holfuyResponse = JsonSerializer.Deserialize<HolfuyResponse>(content);
            if (holfuyResponse == null || holfuyResponse.Measurements == null)
            {
                _logger.LogError("Holfuy: Failed to deserialize response to HolfuyResponse");
                throw new JsonException("Failed to deserialize Holfuy API response to HolfuyResponse.");
            }

            // Validate and filter stations with valid coordinates
            var validStations = holfuyResponse.Measurements
                .Where(station =>
                {
                    if (!double.TryParse(station.Location.Latitude, out var lat) ||
                        !double.TryParse(station.Location.Longitude, out var lng))
                    {
                        return false;
                    }

                    return lat != 0
                        && lng != 0
                        && lat >= -90
                        && lat <= 90
                        && lng >= -180
                        && lng <= 180;
                })
                .ToList();
            _logger.LogInformation("Holfuy: Fetched data for {StationCount} stations", validStations.Count);
            // Map to database models
            var stationData = _holfuyMapping.MapHolfuyToStationData(validStations);
            var weatherStations = _holfuyMapping.MapHolfuyToWeatherStation(validStations);

            _logger.LogInformation("Holfuy: Successfully mapped {StationCount} weather stations and {DataCount} station data records",
                weatherStations.Count, stationData.Count);

            return new HolfuyDataResult
            {
                StationData = stationData,
                WeatherStations = weatherStations
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Holfuy: HTTP error while fetching data: {ErrorMessage}", ex.Message);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Holfuy: Invalid JSON response received");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Holfuy: Unexpected error while fetching data");
            throw;
        }
    }
}


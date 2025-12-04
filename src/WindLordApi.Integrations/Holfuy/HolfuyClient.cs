using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WindLordApi.Data.Models;

namespace WindLordApi.Integrations.Holfuy;

/// <summary>
/// Client for fetching weather station data from Holfuy API
/// </summary>
public class HolfuyClient : IHolfuyClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly HolfuyOptions _options;
    private readonly ILogger<HolfuyClient> _logger;
    private readonly bool _ownsHttpClient;

    private const string BaseUrl = "https://api.holfuy.com/live/";

    public HolfuyClient(
        HttpClient httpClient,
        IOptions<HolfuyOptions> options,
        IConfiguration configuration,
        ILogger<HolfuyClient> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Configure proxy if FIXIE_URL is available
        var proxyUrl = configuration.GetConnectionString("FIXIE_URL");
        if (!string.IsNullOrWhiteSpace(proxyUrl))
        {
            try
            {
                var proxyHandler = CreateProxyHandler(proxyUrl);
                // Create a new HttpClient with proxy support
                _httpClient = new HttpClient(proxyHandler, disposeHandler: true);
                _ownsHttpClient = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure proxy. Using injected HttpClient without proxy.");
                _httpClient = httpClient;
                _ownsHttpClient = false;
            }
        }
        else
        {
            _logger.LogWarning("FIXIE_URL connection string is not configured. Requests will not use a proxy.");
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }
    }

    /// <summary>
    /// Creates an HttpClientHandler configured with proxy support using FIXIE_URL.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the proxy URL is invalid.</exception>
    private HttpClientHandler CreateProxyHandler(string proxyUrl)
    {
        var proxyUri = new Uri(proxyUrl);
        if (string.IsNullOrWhiteSpace(proxyUri.Host) || proxyUri.Port == -1)
        {
            throw new InvalidOperationException("Invalid proxy URL: missing hostname or port");
        }

        // Extract credentials from the proxy URL
        var credentials = proxyUri.UserInfo;
        if (string.IsNullOrWhiteSpace(credentials))
        {
            throw new InvalidOperationException("Invalid proxy URL: missing authentication credentials");
        }

        var credentialParts = credentials.Split(':');
        if (credentialParts.Length != 2)
        {
            throw new InvalidOperationException("Invalid proxy URL: authentication credentials must be in format username:password");
        }

        var proxy = new WebProxy
        {
            Address = new Uri($"http://{proxyUri.Host}:{proxyUri.Port}"),
            Credentials = new NetworkCredential(credentialParts[0], credentialParts[1])
        };

        return new HttpClientHandler
        {
            Proxy = proxy,
            UseProxy = true
        };
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient?.Dispose();
        }
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
                throw new HttpRequestException($"Holfuy API returned error status {response.StatusCode}. Response body: {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Configure JSON deserialization options
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Deserialize directly to array (the API returns an array directly)
            var holfuyData = JsonSerializer.Deserialize<List<HolfuyStationData>>(content, jsonOptions);
            if (holfuyData == null)
            {
                throw new JsonException("Failed to deserialize Holfuy API response to List<HolfuyStationData>.");
            }

            // Validate and filter stations with valid coordinates
            var validStations = holfuyData
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

            // Map to database models
            var stationData = HolfuyMapping.MapHolfuyToStationData(validStations);
            var weatherStations = HolfuyMapping.MapHolfuyToWeatherStation(validStations);

            return new HolfuyDataResult
            {
                StationData = stationData,
                WeatherStations = weatherStations
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching Holfuy data: {ErrorMessage}", ex.Message);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON response received from Holfuy API");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching Holfuy data");
            throw;
        }
    }
}


using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WindLordApi.Integrations.MetFrost;

/// <summary>
/// Client for fetching weather station data from MET Frost API
/// </summary>
public class MetFrostClient : IMetFrostClient
{
    private readonly HttpClient _httpClient;
    private readonly MetFrostOptions _options;
    private readonly ILogger<MetFrostClient> _logger;

    // Query parameters constants
    private const string BaseUrl = "https://frost.met.no/observations/v0.jsonld";
    private const string SourcesUrl = "https://frost.met.no/sources/v0.jsonld";
    private const string TimeRange = "latest";
    private static readonly string[] Elements =
    [
        "wind_speed",
        "wind_from_direction",
        "max(wind_speed_of_gust PT10M)", // 10-minute resolution (preferred, but not available for all stations)
        "max(wind_speed_of_gust PT1H)", // Hourly resolution (fallback for stations without PT10M)
        "air_temperature"
    ];

    public MetFrostClient(
        HttpClient httpClient,
        IOptions<MetFrostOptions> options,
        ILogger<MetFrostClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets the latest station data from MET Frost API
    /// </summary>
    /// <param name="stationIds">Array of station IDs to fetch data for (should be <= 100 stations)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized MET observations response</returns>
    public async Task<MetObservationsResponse> FetchMetStationDataAsync(
        string[] stationIds,
        CancellationToken cancellationToken = default)
    {
        if (stationIds == null || stationIds.Length == 0)
        {
            // Return an empty, but valid, response object when there are no station IDs.
            return new MetObservationsResponse
            {
                Context = string.Empty,
                Type = string.Empty,
                ApiVersion = string.Empty,
                License = new Uri("https://example.com"),
                CreatedAt = DateTimeOffset.UtcNow,
                QueryTime = 0,
                CurrentItemCount = 0,
                ItemsPerPage = 0,
                Offset = 0,
                TotalItemCount = 0,
                Data = Array.Empty<MetObservationsData>()
            };
        }

        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            _logger.LogError("MetFrost: ClientId is not configured");
            throw new InvalidOperationException("MET ClientId is not configured");
        }

        // Create Basic auth header
        var authBytes = Encoding.UTF8.GetBytes($"{_options.ClientId}:");
        var authHeader = Convert.ToBase64String(authBytes);

        // Build query parameters
        var queryParams = new List<string>
        {
            $"sources={Uri.EscapeDataString(string.Join(",", stationIds))}",
            $"referencetime={Uri.EscapeDataString(TimeRange)}",
            $"elements={Uri.EscapeDataString(string.Join(",", Elements))}"
        };

        var queryString = string.Join("&", queryParams);
        var requestUrl = $"{BaseUrl}?{queryString}";


        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("User-Agent", "WindAlert/1.0");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        _logger.LogInformation("MetFrost: Fetching station data for {StationCount} station(s)", stationIds.Length);
        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("MetFrost: Returned error status {StatusCode}. Response body: {ErrorContent}", response.StatusCode, errorContent);
                throw new HttpRequestException($"MET Frost API returned error status {response.StatusCode}. Response body: {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<MetObservationsResponse>(content);
            if (result is null)
            {
                _logger.LogError("MetFrost: Failed to deserialize response to MetObservationsResponse");
                throw new JsonException("Failed to deserialize MET Frost API response to MetObservationsResponse.");
            }

            _logger.LogInformation("MetFrost: Successfully fetched station data with {DataCount} observation data points", result.Data.Count);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "MetFrost: HTTP error while fetching station data: {ErrorMessage}", ex.Message);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "MetFrost: Invalid JSON response received");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MetFrost: Unexpected error while fetching station data");
            throw;
        }
    }

    /// <summary>
    /// Fetches all available weather stations from MET Frost API.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deserialized MET stations response.</returns>
    public async Task<MetFrostStationsResponse> FetchMetFrostStationsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            _logger.LogError("MetFrost: ClientId is not configured");
            throw new InvalidOperationException("MET ClientId is not configured");
        }

        // Create Basic auth header
        var authBytes = Encoding.UTF8.GetBytes($"{_options.ClientId}:");
        var authHeader = Convert.ToBase64String(authBytes);

        using var request = new HttpRequestMessage(HttpMethod.Get, SourcesUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("User-Agent", "WindAlert/1.0");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        _logger.LogInformation("MetFrost: Fetching weather stations");
        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("MetFrost: Returned error status {StatusCode} while fetching stations. Response body: {ErrorContent}", response.StatusCode, errorContent);
                throw new HttpRequestException($"MET Frost API returned error status {response.StatusCode}. Response body: {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<MetFrostStationsResponse>(content);
            if (result is null)
            {
                _logger.LogError("MetFrost: Failed to deserialize response to MetFrostStationsResponse");
                throw new JsonException("Failed to deserialize MET Frost API response to MetFrostStationsResponse.");
            }

            _logger.LogInformation("MetFrost: Successfully fetched {StationCount} weather stations", result.Data.Count);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "MetFrost: HTTP error while fetching stations: {ErrorMessage}", ex.Message);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "MetFrost: Invalid JSON response received");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MetFrost: Unexpected error while fetching stations");
            throw;
        }
    }
}


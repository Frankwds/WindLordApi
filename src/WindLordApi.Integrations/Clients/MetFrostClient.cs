using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WindLordApi.Integrations.Clients;

/// <summary>
/// Client for fetching weather station data from MET Frost API
/// </summary>
public class MetFrostClient
{
    private readonly HttpClient _httpClient;
    private readonly MetFrostOptions _options;
    private readonly ILogger<MetFrostClient> _logger;

    // Query parameters constants
    private const string BaseUrl = "https://frost.met.no/observations/v0.jsonld";
    private const string TimeRange = "latest";
    private static readonly string[] Elements = new[]
    {
        "wind_speed",
        "wind_from_direction",
        "max(wind_speed_of_gust PT10M)", // 10-minute resolution (preferred, but not available for all stations)
        "max(wind_speed_of_gust PT1H)", // Hourly resolution (fallback for stations without PT10M)
        "air_temperature"
    };

    public MetFrostClient(
        HttpClient httpClient,
        IOptions<MetFrostOptions> options,
        ILogger<MetFrostClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        // Set timeout (30 seconds default, matching Next.js implementation)
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Gets the last 10 minutes of station data from MET Frost API
    /// </summary>
    /// <param name="stationIds">Array of station IDs to fetch data for (should be <= 100 stations)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Raw JSON response as JsonDocument</returns>
    public async Task<JsonDocument> FetchMetStationDataAsync(
        string[] stationIds,
        CancellationToken cancellationToken = default)
    {
        if (stationIds == null || stationIds.Length == 0)
        {
            return JsonDocument.Parse("[]");
        }

        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
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

        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("User-Agent", "WindAlert/1.0");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        _logger.LogInformation("Fetching MET Frost data for {Count} stations", stationIds.Length);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDocument = JsonDocument.Parse(content);

            _logger.LogInformation("Successfully fetched MET Frost data");
            return jsonDocument;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching MET Frost data");
            throw;
        }
    }
}


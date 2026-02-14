using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WindLordApi.Integrations.WindsMobi;

/// <summary>
/// Client for fetching weather station data from WindsMobi API.
/// Iterates through all configured providers sequentially.
/// </summary>
public class WindsMobiClient : IWindsMobiClient
{
    private readonly HttpClient _httpClient;
    private readonly IWindsMobiMapping _mapping;
    private readonly ILogger<WindsMobiClient> _logger;

    private const string BaseUrl = "https://winds.mobi/api/2.3/stations/";

    /// <summary>
    /// Delay between provider requests to respect fair-use policy.
    /// </summary>
    private static readonly TimeSpan DelayBetweenRequests = TimeSpan.FromSeconds(1);

    /// <summary>
    /// All WindsMobi sub-providers to poll.
    /// </summary>
    private static readonly string[] Providers =
    [
        "aletsch", "borntofly", "ffvl", "gxaircom",
        "iweathar", "kachelmannwetter", "metar", "meteoswiss",
        "pdcs", "pgsonda", "pioupiou", "pmcjoder",
        "slf", "thunerwetter", "windball", "windline", "windspots",
        "windy", "wunderground", "yvbeach", "zermatt"
    ];

    public WindsMobiClient(
        HttpClient httpClient,
        IWindsMobiMapping mapping,
        ILogger<WindsMobiClient> logger)
    {
        _httpClient = httpClient;
        _mapping = mapping;
        _logger = logger;
    }

    /// <summary>
    /// Fetches station data from all configured WindsMobi providers.
    /// Iterates through each provider sequentially with a delay between requests.
    /// </summary>
    public async Task<WindsMobiDataResult> FetchAllProvidersAsync(CancellationToken cancellationToken = default)
    {
        var allStations = new List<WindsMobiStation>();

        _logger.LogInformation("WindsMobi: Starting fetch for {ProviderCount} providers", Providers.Length);

        for (var i = 0; i < Providers.Length; i++)
        {
            var provider = Providers[i];

            try
            {
                var stations = await FetchProviderAsync(provider, cancellationToken);
                allStations.AddRange(stations);

                _logger.LogDebug("WindsMobi: Provider '{Provider}' returned {Count} stations ({Current}/{Total})",
                    provider, stations.Count, i + 1, Providers.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WindsMobi: Error fetching provider '{Provider}' ({Current}/{Total}), skipping",
                    provider, i + 1, Providers.Length);
                // Continue with next provider instead of failing completely
            }

            // Delay between requests to respect fair-use policy (skip after last provider)
            if (i < Providers.Length - 1)
            {
                await Task.Delay(DelayBetweenRequests, cancellationToken);
            }
        }

        _logger.LogInformation("WindsMobi: Fetched {TotalStations} stations across {ProviderCount} providers",
            allStations.Count, Providers.Length);

        // Map all collected stations to domain models
        var stationData = _mapping.MapToStationData(allStations);
        var weatherStations = _mapping.MapToWeatherStation(allStations);

        _logger.LogDebug("WindsMobi: Mapped {StationCount} weather stations and {DataCount} station data records",
            weatherStations.Count, stationData.Count);

        return new WindsMobiDataResult
        {
            StationData = stationData,
            WeatherStations = weatherStations
        };
    }

    /// <summary>
    /// Fetches station data for a single provider from the WindsMobi API.
    /// </summary>
    private async Task<List<WindsMobiStation>> FetchProviderAsync(string provider, CancellationToken cancellationToken)
    {
        var requestUrl = $"{BaseUrl}?limit=500" +
            "&keys=pv-code&keys=short&keys=alt&keys=loc" +
            "&keys=last._id&keys=last.w-dir&keys=last.w-avg&keys=last.w-max&keys=last.temp" +
            $"&provider={Uri.EscapeDataString(provider)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("User-Agent", "WindLordApi/1.0");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("WindsMobi: Provider '{Provider}' returned error status {StatusCode}. Response: {ErrorContent}",
                provider, response.StatusCode, errorContent);
            throw new HttpRequestException(
                $"WindsMobi API returned error status {response.StatusCode} for provider '{provider}'. Response: {errorContent}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var stations = JsonSerializer.Deserialize<List<WindsMobiStation>>(content);
        if (stations == null)
        {
            _logger.LogWarning("WindsMobi: Failed to deserialize response for provider '{Provider}'", provider);
            return [];
        }

        return stations;
    }
}

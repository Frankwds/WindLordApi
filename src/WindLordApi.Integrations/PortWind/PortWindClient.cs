using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WindLordApi.Integrations.PortWind;

public class PortWindClient : IPortWindClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly PortWindOptions _options;
    private readonly ILogger<PortWindClient> _logger;

    public PortWindClient(
        HttpClient httpClient,
        IOptions<PortWindOptions> options,
        ILogger<PortWindClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, PortWindStationDto>> FetchStationsAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _options.StationListUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/javascript"));
        request.Headers.Add("User-Agent", "WindLordApi/1.0");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonObject = PortWindStationListParser.ExtractJsonObject(content);
        var stations = JsonSerializer.Deserialize<Dictionary<string, PortWindStationDto>>(jsonObject, SerializerOptions);
        if (stations == null)
        {
            throw new FormatException("PortWind station payload could not be deserialized into station metadata");
        }

        _logger.LogInformation("PortWind: Parsed {Count} station records from station list", stations.Count);
        return stations;
    }

    public async Task<PortWindObservationResponseDto> FetchLatestAndPreviousObservationAsync(string stationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stationId))
        {
            throw new ArgumentException("Station ID cannot be null or empty", nameof(stationId));
        }

        var requestUrl = BuildObservationUrl(stationId);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("User-Agent", "WindLordApi/1.0");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<PortWindObservationResponseDto>(responseStream, SerializerOptions, cancellationToken);
        return payload ?? new PortWindObservationResponseDto();
    }

    private string BuildObservationUrl(string stationId)
    {
        var separator = _options.ObservationBaseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{_options.ObservationBaseUrl}{separator}stationid={Uri.EscapeDataString(stationId)}&dataset=latestandprevious";
    }
}
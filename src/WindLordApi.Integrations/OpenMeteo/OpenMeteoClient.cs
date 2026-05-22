using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Client for fetching batched forecast data from Open-Meteo.
/// </summary>
public class OpenMeteoClient : IOpenMeteoClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly OpenMeteoOptions _options;
    private readonly ILogger<OpenMeteoClient> _logger;

    public OpenMeteoClient(
        HttpClient httpClient,
        IOptions<OpenMeteoOptions> options,
        ILogger<OpenMeteoClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OpenMeteoForecastResponse>> FetchForecastAsync(
        IReadOnlyList<OpenMeteoRequestLocation> locations,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locations);

        if (locations.Count == 0)
        {
            throw new ArgumentException("Open-Meteo forecast batch must contain at least one location.", nameof(locations));
        }

        if (endUtc <= startUtc)
        {
            throw new ArgumentException("Open-Meteo forecast end time must be later than the start time.", nameof(endUtc));
        }

        var requestUrl = BuildRequestUrl(locations, startUtc, endUtc);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("User-Agent", "WindLordApi/1.0");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "OpenMeteo: Returned error status {StatusCode} for {LocationCount} locations. Response body: {ErrorContent}",
                    response.StatusCode,
                    locations.Count,
                    errorContent);
                throw new HttpRequestException($"Open-Meteo forecast endpoint returned status {response.StatusCode}. Response body: {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return DeserializeResponse(content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OpenMeteo: HTTP error while fetching batched forecast data for {LocationCount} locations", locations.Count);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "OpenMeteo: Invalid JSON response received for {LocationCount} locations", locations.Count);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenMeteo: Unexpected error while fetching batched forecast data for {LocationCount} locations", locations.Count);
            throw;
        }
    }

    private string BuildRequestUrl(IReadOnlyList<OpenMeteoRequestLocation> locations, DateTime startUtc, DateTime endUtc)
    {
        var latitudes = string.Join(",", locations.Select(location => FormatCoordinate(OpenMeteoCoordinates.TruncateToRequestPrecision(location.Latitude))));
        var longitudes = string.Join(",", locations.Select(location => FormatCoordinate(OpenMeteoCoordinates.TruncateToRequestPrecision(location.Longitude))));

        var queryParameters = new Dictionary<string, string>
        {
            ["latitude"] = latitudes,
            ["longitude"] = longitudes,
            ["hourly"] = "temperature_2m,wind_speed_10m,wind_direction_10m,wind_gusts_10m,precipitation,precipitation_probability,pressure_msl,weather_code,is_day",
            ["start_hour"] = startUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm"),
            ["end_hour"] = endUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm"),
            ["wind_speed_unit"] = "ms",
            ["timezone"] = "GMT"
        };

        var queryString = string.Join("&", queryParameters.Select(parameter => $"{parameter.Key}={Uri.EscapeDataString(parameter.Value)}"));
        return $"{_options.BaseUrl}?{queryString}";
    }

    private static string FormatCoordinate(double value)
    {
        return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<OpenMeteoForecastResponse> DeserializeResponse(string content)
    {
        using var jsonDocument = JsonDocument.Parse(content);

        return jsonDocument.RootElement.ValueKind switch
        {
            JsonValueKind.Array => JsonSerializer.Deserialize<List<OpenMeteoForecastResponse>>(content, JsonOptions)
                ?? throw new JsonException("Failed to deserialize Open-Meteo forecast array response."),
            JsonValueKind.Object => new[]
            {
                JsonSerializer.Deserialize<OpenMeteoForecastResponse>(content, JsonOptions)
                    ?? throw new JsonException("Failed to deserialize Open-Meteo forecast object response.")
            },
            _ => throw new JsonException("Open-Meteo forecast response must be either a JSON object or array.")
        };
    }
}
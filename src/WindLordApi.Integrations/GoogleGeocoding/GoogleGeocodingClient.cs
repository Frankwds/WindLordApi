using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WindLordApi.Integrations.GoogleGeocoding;

/// <summary>
/// Client for reverse geocoding coordinates to country names using the Google Geocoding API.
/// </summary>
public class GoogleGeocodingClient : IGoogleGeocodingClient
{
    private readonly HttpClient _httpClient;
    private readonly GoogleGeocodingOptions _options;
    private readonly ILogger<GoogleGeocodingClient> _logger;

    private const string BaseUrl = "https://maps.googleapis.com/maps/api/geocode/json";

    public GoogleGeocodingClient(
        HttpClient httpClient,
        IOptions<GoogleGeocodingOptions> options,
        ILogger<GoogleGeocodingClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> ReverseGeocodeCountryAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
    {
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lng = longitude.ToString(CultureInfo.InvariantCulture);

        var requestUrl = $"{BaseUrl}?latlng={lat},{lng}&result_type=country&key={_options.ApiKey}";

        try
        {
            using var response = await _httpClient.GetAsync(requestUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "GoogleGeocoding: API returned {StatusCode} for coordinates ({Lat}, {Lng}). Response: {Error}",
                    response.StatusCode, lat, lng, errorContent);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseCountryFromResponse(content, lat, lng);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GoogleGeocoding: Error reverse geocoding coordinates ({Lat}, {Lng})", lat, lng);
            return null;
        }
    }

    /// <summary>
    /// Parses the country long_name from the Google Geocoding API JSON response.
    /// </summary>
    private string? ParseCountryFromResponse(string json, string lat, string lng)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Check API status
            if (root.TryGetProperty("status", out var statusElement))
            {
                var status = statusElement.GetString();
                if (status != "OK")
                {
                    _logger.LogWarning("GoogleGeocoding: API status '{Status}' for coordinates ({Lat}, {Lng})", status, lat, lng);
                    return null;
                }
            }

            // Navigate: results[0].address_components[] -> find type "country" -> long_name
            if (!root.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            {
                _logger.LogWarning("GoogleGeocoding: No results for coordinates ({Lat}, {Lng})", lat, lng);
                return null;
            }

            var firstResult = results[0];
            if (!firstResult.TryGetProperty("address_components", out var addressComponents))
            {
                _logger.LogWarning("GoogleGeocoding: No address_components for coordinates ({Lat}, {Lng})", lat, lng);
                return null;
            }

            foreach (var component in addressComponents.EnumerateArray())
            {
                if (!component.TryGetProperty("types", out var types))
                    continue;

                var isCountry = false;
                foreach (var type in types.EnumerateArray())
                {
                    if (type.GetString() == "country")
                    {
                        isCountry = true;
                        break;
                    }
                }

                if (isCountry && component.TryGetProperty("long_name", out var longName))
                {
                    return longName.GetString();
                }
            }

            _logger.LogWarning("GoogleGeocoding: No country component found for coordinates ({Lat}, {Lng})", lat, lng);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "GoogleGeocoding: Failed to parse JSON response for coordinates ({Lat}, {Lng})", lat, lng);
            return null;
        }
    }
}

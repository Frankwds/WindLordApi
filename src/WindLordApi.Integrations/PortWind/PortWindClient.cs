using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WindLordApi.Integrations.PortWind;

/// <summary>
/// Client for fetching PortWind station metadata and observations.
/// </summary>
public class PortWindClient : IPortWindClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
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

    public async Task<IReadOnlyDictionary<string, PortWindStationCatalogEntry>> FetchStationsAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _options.StationCatalogUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/javascript"));
        request.Headers.Add("User-Agent", "WindLordApi/1.0");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("PortWind: Station catalog returned status {StatusCode}. Response: {ErrorContent}", response.StatusCode, errorContent);
            throw new HttpRequestException($"PortWind station catalog returned status {response.StatusCode}. Response: {errorContent}");
        }

        var rawBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var scriptContent = Encoding.UTF8.GetString(rawBytes);
        var objectLiteral = ExtractStationsObjectLiteral(scriptContent);
        var stationCatalogJson = ConvertJavaScriptObjectLiteralToJson(objectLiteral);

        var stations = JsonSerializer.Deserialize<Dictionary<string, PortWindStationCatalogEntry>>(stationCatalogJson, JsonOptions);
        if (stations is null)
        {
            throw new JsonException("Failed to deserialize PortWind station catalog.");
        }

        return stations;
    }

    public async Task<PortWindLatestResponse?> FetchLatestDataAsync(string stationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stationId))
        {
            throw new ArgumentException("Station ID cannot be null or empty", nameof(stationId));
        }

        var requestUrl = $"{_options.LatestDataBaseUrl}?stationid={Uri.EscapeDataString(stationId)}&dataset=latest";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("User-Agent", "WindLordApi/1.0");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("PortWind: Latest data for {StationId} returned status {StatusCode}. Response: {ErrorContent}", stationId, response.StatusCode, errorContent);
            throw new HttpRequestException($"PortWind latest data returned status {response.StatusCode} for station '{stationId}'. Response: {errorContent}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var latestData = JsonSerializer.Deserialize<PortWindLatestResponse>(content, JsonOptions);
        if (latestData is null)
        {
            _logger.LogWarning("PortWind: Latest data response for {StationId} could not be deserialized", stationId);
        }

        return latestData;
    }

    private static string ExtractStationsObjectLiteral(string scriptContent)
    {
        const string symbol = "window.stations";

        var symbolIndex = scriptContent.IndexOf(symbol, StringComparison.Ordinal);
        if (symbolIndex < 0)
        {
            throw new FormatException("PortWind station catalog is missing the window.stations assignment.");
        }

        var assignmentIndex = scriptContent.IndexOf('=', symbolIndex + symbol.Length);
        if (assignmentIndex < 0)
        {
            throw new FormatException("PortWind station catalog is missing the assignment operator for window.stations.");
        }

        var objectStartIndex = scriptContent.IndexOf('{', assignmentIndex + 1);
        if (objectStartIndex < 0)
        {
            throw new FormatException("PortWind station catalog is missing the assigned object literal.");
        }

        var depth = 0;
        var inString = false;
        var escaping = false;

        for (var i = objectStartIndex; i < scriptContent.Length; i++)
        {
            var current = scriptContent[i];

            if (inString)
            {
                if (escaping)
                {
                    escaping = false;
                }
                else if (current == '\\')
                {
                    escaping = true;
                }
                else if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (current == '"')
            {
                inString = true;
                continue;
            }

            if (current == '{')
            {
                depth++;
            }
            else if (current == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return scriptContent.Substring(objectStartIndex, i - objectStartIndex + 1);
                }
            }
        }

        throw new FormatException("PortWind station catalog object literal is not balanced.");
    }

    private static string ConvertJavaScriptObjectLiteralToJson(string objectLiteral)
    {
        var output = new StringBuilder(objectLiteral.Length + 32);
        var contexts = new Stack<ContainerContext>();
        var inString = false;
        var escaping = false;

        for (var i = 0; i < objectLiteral.Length; i++)
        {
            var current = objectLiteral[i];

            if (inString)
            {
                output.Append(current);

                if (escaping)
                {
                    escaping = false;
                }
                else if (current == '\\')
                {
                    escaping = true;
                }
                else if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            switch (current)
            {
                case '"':
                    inString = true;
                    output.Append(current);
                    break;

                case '{':
                    contexts.Push(new ContainerContext(isObject: true, expectKey: true));
                    output.Append(current);
                    break;

                case '[':
                    contexts.Push(new ContainerContext(isObject: false, expectKey: false));
                    output.Append(current);
                    break;

                case '}':
                case ']':
                    if (contexts.Count > 0)
                    {
                        contexts.Pop();
                    }

                    output.Append(current);
                    break;

                case ':':
                    if (contexts.Count > 0 && contexts.Peek().IsObject)
                    {
                        contexts.Peek().ExpectKey = false;
                    }

                    output.Append(current);
                    break;

                case ',':
                    if (contexts.Count > 0 && contexts.Peek().IsObject)
                    {
                        contexts.Peek().ExpectKey = true;
                    }

                    output.Append(current);
                    break;

                default:
                    if (char.IsWhiteSpace(current))
                    {
                        output.Append(current);
                        break;
                    }

                    if (ShouldQuoteIdentifier(contexts, current))
                    {
                        var startIndex = i;
                        while (i < objectLiteral.Length && IsIdentifierPart(objectLiteral[i]))
                        {
                            i++;
                        }

                        var identifier = objectLiteral.Substring(startIndex, i - startIndex);
                        output.Append('"').Append(identifier).Append('"');
                        i--;
                        break;
                    }

                    output.Append(current);
                    break;
            }
        }

        return output.ToString();
    }

    private static bool ShouldQuoteIdentifier(Stack<ContainerContext> contexts, char current)
    {
        return contexts.Count > 0
            && contexts.Peek().IsObject
            && contexts.Peek().ExpectKey
            && IsIdentifierStart(current);
    }

    private static bool IsIdentifierStart(char value)
    {
        return char.IsLetter(value) || value == '_' || value == '$';
    }

    private static bool IsIdentifierPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_' || value == '$';
    }

    private sealed class ContainerContext(bool isObject, bool expectKey)
    {
        public bool IsObject { get; } = isObject;

        public bool ExpectKey { get; set; } = expectKey;
    }
}
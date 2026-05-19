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
        var stringDelimiter = '\0';
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
                else if (current == stringDelimiter)
                {
                    inString = false;
                    stringDelimiter = '\0';
                }

                continue;
            }

            if (current is '\'' or '"')
            {
                inString = true;
                stringDelimiter = current;
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
        var builder = new StringBuilder(objectLiteral.Length + 32);

        for (var index = 0; index < objectLiteral.Length;)
        {
            var current = objectLiteral[index];

            if (current is '\'' or '"')
            {
                builder.Append(JsonSerializer.Serialize(ReadQuotedString(objectLiteral, ref index)));
                continue;
            }

            if (current is '{' or ',')
            {
                builder.Append(current);
                index++;

                while (index < objectLiteral.Length && char.IsWhiteSpace(objectLiteral[index]))
                {
                    builder.Append(objectLiteral[index]);
                    index++;
                }

                if (TryReadPropertyIdentifier(objectLiteral, ref index, out var identifier))
                {
                    builder.Append('"').Append(identifier).Append('"');
                    continue;
                }

                continue;
            }

            if (current is '}' or ']')
            {
                RemoveTrailingComma(builder);
                builder.Append(current);
                index++;
                continue;
            }

            builder.Append(current);
            index++;
        }

        return builder.ToString();
    }

    private static string ReadQuotedString(string input, ref int index)
    {
        var delimiter = input[index++];
        var builder = new StringBuilder();

        while (index < input.Length)
        {
            var current = input[index++];

            if (current == '\\')
            {
                if (index >= input.Length)
                {
                    throw new FormatException("PortWind station catalog contained an unterminated escape sequence.");
                }

                var escaped = input[index++];
                builder.Append(escaped switch
                {
                    '\\' => '\\',
                    '\'' => '\'',
                    '"' => '"',
                    '/' => '/',
                    'b' => '\b',
                    'f' => '\f',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    'u' => ReadUnicodeEscape(input, ref index),
                    _ => escaped
                });
                continue;
            }

            if (current == delimiter)
            {
                return builder.ToString();
            }

            builder.Append(current);
        }

        throw new FormatException("PortWind station catalog contained an unterminated string.");
    }

    private static char ReadUnicodeEscape(string input, ref int index)
    {
        if (index + 4 > input.Length)
        {
            throw new FormatException("PortWind station catalog contained an incomplete unicode escape.");
        }

        var hex = input.Substring(index, 4);
        index += 4;
        if (!ushort.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException("PortWind station catalog contained an invalid unicode escape.");
        }

        return (char)value;
    }

    private static bool TryReadPropertyIdentifier(string input, ref int index, out string identifier)
    {
        identifier = string.Empty;
        if (index >= input.Length)
        {
            return false;
        }

        if (!(char.IsLetter(input[index]) || input[index] is '_' or '$'))
        {
            return false;
        }

        var start = index;
        index++;
        while (index < input.Length && (char.IsLetterOrDigit(input[index]) || input[index] is '_' or '$'))
        {
            index++;
        }

        var end = index;
        while (index < input.Length && char.IsWhiteSpace(input[index]))
        {
            index++;
        }

        if (index >= input.Length || input[index] != ':')
        {
            index = start;
            return false;
        }

        identifier = input[start..end];
        return true;
    }

    private static void RemoveTrailingComma(StringBuilder builder)
    {
        for (var index = builder.Length - 1; index >= 0; index--)
        {
            if (char.IsWhiteSpace(builder[index]))
            {
                continue;
            }

            if (builder[index] == ',')
            {
                builder.Remove(index, 1);
            }

            break;
        }
    }
}
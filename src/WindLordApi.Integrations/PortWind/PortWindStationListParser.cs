using System.Text;
using System.Text.Json;

namespace WindLordApi.Integrations.PortWind;

public static class PortWindStationListParser
{
    private const string AssignmentToken = "window.stations";

    public static string ExtractJsonObject(string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            throw new ArgumentException("PortWind station payload cannot be null or empty", nameof(rawPayload));
        }

        var assignmentIndex = rawPayload.IndexOf(AssignmentToken, StringComparison.Ordinal);
        if (assignmentIndex < 0)
        {
            throw new FormatException("PortWind station payload did not contain a window.stations assignment");
        }

        var equalsIndex = rawPayload.IndexOf('=', assignmentIndex + AssignmentToken.Length);
        if (equalsIndex < 0)
        {
            throw new FormatException("PortWind station payload did not contain an assignment operator after window.stations");
        }

        var objectStart = rawPayload.IndexOf('{', equalsIndex + 1);
        if (objectStart < 0)
        {
            throw new FormatException("PortWind station payload did not contain a station object");
        }

        var objectLiteral = ExtractBalancedObject(rawPayload, objectStart);
        return NormalizeJavaScriptObjectLiteral(objectLiteral);
    }

    private static string ExtractBalancedObject(string payload, int objectStart)
    {
        var depth = 0;
        var inString = false;
        var stringDelimiter = '\0';
        var escaped = false;

        for (var index = objectStart; index < payload.Length; index++)
        {
            var current = payload[index];

            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (current == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (current == stringDelimiter)
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
                continue;
            }

            if (current == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return payload[objectStart..(index + 1)];
                }
            }
        }

        throw new FormatException("PortWind station payload contained an unterminated station object");
    }

    private static string NormalizeJavaScriptObjectLiteral(string objectLiteral)
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
                    throw new FormatException("PortWind station payload contained an unterminated escape sequence");
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

        throw new FormatException("PortWind station payload contained an unterminated string");
    }

    private static char ReadUnicodeEscape(string input, ref int index)
    {
        if (index + 4 > input.Length)
        {
            throw new FormatException("PortWind station payload contained an incomplete unicode escape");
        }

        var hex = input.Substring(index, 4);
        index += 4;
        if (!ushort.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException("PortWind station payload contained an invalid unicode escape");
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
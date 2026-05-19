using System.Globalization;
using System.Text;
using System.Text.Json;
using WindLordApi.Data.Models;

namespace WindLordApi.Integrations.PortWind;

public class PortWindMappingService : IPortWindMapping
{
    public List<WeatherStation> MapStations(IReadOnlyDictionary<string, PortWindStationDto> stations)
    {
        return stations
            .Select(station => MapStation(station.Key, station.Value))
            .Where(weatherStation => weatherStation is not null)
            .Select(weatherStation => weatherStation!)
            .ToList();
    }

    public List<StationData> MapObservations(string stationId, IReadOnlyList<PortWindObservationDto> observations)
    {
        if (string.IsNullOrWhiteSpace(stationId))
        {
            throw new ArgumentException("Station ID cannot be null or empty", nameof(stationId));
        }

        return observations
            .Select(observation => MapObservation(stationId, observation))
            .Where(stationData => stationData is not null)
            .Select(stationData => stationData!)
            .ToList();
    }

    public string NormalizeStationLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return label;
        }

        var trimmed = label.Trim();
        if (!ContainsMojibake(trimmed))
        {
            return trimmed;
        }

        var repaired = Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-1").GetBytes(trimmed));
        return CountMojibakeMarkers(repaired) < CountMojibakeMarkers(trimmed)
            ? repaired
            : trimmed;
    }

    private WeatherStation? MapStation(string stationId, PortWindStationDto station)
    {
        if (string.IsNullOrWhiteSpace(stationId))
        {
            return null;
        }

        if (!TryGetCoordinates(station.Properties, out var latitude, out var longitude))
        {
            return null;
        }

        if (!IsValidCoordinates(latitude, longitude))
        {
            return null;
        }

        int? altitude = TryGetInt(station.Properties, out var parsedAltitude, "alt", "altitude", "elevation")
            ? parsedAltitude
            : null;
        var name = GetString(station.Properties, "label", "name", "title", "station_name") ?? stationId.Trim();
        var hasStatus = TryGetBool(station.Properties, out var status, "status");
        var hasHistory = TryGetBool(station.Properties, out var history, "history");

        return new WeatherStation
        {
            StationId = stationId.Trim(),
            Name = NormalizeStationLabel(name),
            Latitude = Math.Round(latitude, 5),
            Longitude = Math.Round(longitude, 5),
            Altitude = altitude,
            Country = null,
            IsActive = hasStatus && hasHistory && status && history,
            Provider = PortWindOptions.ProviderName,
            IsMain = false
        };
    }

    private StationData? MapObservation(string stationId, PortWindObservationDto observation)
    {
        if (!observation.Uts.HasValue)
        {
            return null;
        }

        if (!TryGetDecimal(observation.Properties, out var windSpeed, "wind_speed_avg", "windspeed_avg", "wind_avg"))
        {
            return null;
        }

        DateTime updatedAt;
        try
        {
            updatedAt = DateTimeOffset.FromUnixTimeMilliseconds(observation.Uts.Value).UtcDateTime;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }

        var direction = TryGetInt(observation.Properties, out var rawDirection, "wind_direction_avg", "winddirection_avg", "wind_direction", "direction")
            ? NormalizeDirection(rawDirection)
            : 0;

        decimal? windGust = TryGetDecimal(observation.Properties, out var gust, "wind_gust", "wind_speed_max", "wind_gust_max", "gust_max", "wind_max")
            ? gust
            : null;

        decimal? windMinSpeed = TryGetDecimal(observation.Properties, out var minSpeed, "wind_speed_min", "windspeed_min", "wind_min")
            ? minSpeed
            : null;

        decimal? temperature = TryGetDecimal(observation.Properties, out var temperatureAverage, "temperature_avg")
            ? temperatureAverage
            : null;

        return new StationData
        {
            StationId = stationId,
            WindSpeed = windSpeed,
            WindGust = windGust,
            WindMinSpeed = windMinSpeed,
            Direction = direction,
            Temperature = temperature,
            UpdatedAt = updatedAt,
            IsCompressed = false
        };
    }

    private static bool IsValidCoordinates(decimal latitude, decimal longitude)
    {
        return latitude != 0
            && longitude != 0
            && latitude >= -90
            && latitude <= 90
            && longitude >= -180
            && longitude <= 180;
    }

    private static bool TryGetCoordinates(IReadOnlyDictionary<string, JsonElement> properties, out decimal latitude, out decimal longitude)
    {
        latitude = default;
        longitude = default;

        if (TryGetDecimal(properties, out latitude, "lat", "latitude") &&
            TryGetDecimal(properties, out longitude, "lng", "lon", "longitude"))
        {
            return true;
        }

        if (!TryGetElement(properties, out var locationElement, "location") || locationElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var locationProperties = locationElement.EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value, StringComparer.OrdinalIgnoreCase);

        return TryGetDecimal(locationProperties, out latitude, "lat", "latitude") &&
            TryGetDecimal(locationProperties, out longitude, "lng", "lon", "longitude");
    }

    private static int NormalizeDirection(int direction)
    {
        return ((direction % 360) + 360) % 360;
    }

    private static string? GetString(IReadOnlyDictionary<string, JsonElement> properties, params string[] keys)
    {
        if (!TryGetElement(properties, out var element, keys))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => null
        };
    }

    private static bool TryGetInt(IReadOnlyDictionary<string, JsonElement> properties, out int value, params string[] keys)
    {
        value = default;
        if (!TryGetElement(properties, out var element, keys))
        {
            return false;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var decimalValue))
        {
            value = (int)Math.Round(decimalValue, MidpointRounding.AwayFromZero);
            return true;
        }

        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetBool(IReadOnlyDictionary<string, JsonElement> properties, out bool value, params string[] keys)
    {
        value = default;
        if (!TryGetElement(properties, out var element, keys))
        {
            return false;
        }

        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            value = element.GetBoolean();
            return true;
        }

        if (element.ValueKind == JsonValueKind.String && bool.TryParse(element.GetString(), out value))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetDecimal(IReadOnlyDictionary<string, JsonElement> properties, out decimal value, params string[] keys)
    {
        value = default;
        if (!TryGetElement(properties, out var element, keys))
        {
            return false;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.String && decimal.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetElement(IReadOnlyDictionary<string, JsonElement> properties, out JsonElement value, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (properties.TryGetValue(key, out value))
            {
                return true;
            }

            var match = properties.FirstOrDefault(pair => string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(match.Key))
            {
                value = match.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool ContainsMojibake(string value)
    {
        return value.Contains('Ã') || value.Contains('Â') || value.Contains('�');
    }

    private static int CountMojibakeMarkers(string value)
    {
        return value.Count(character => character is 'Ã' or 'Â' or '�');
    }
}
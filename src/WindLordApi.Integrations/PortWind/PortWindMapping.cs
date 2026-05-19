using System.Text;
using System.Text.RegularExpressions;
using WindLordApi.Data.Models;

namespace WindLordApi.Integrations.PortWind;

/// <summary>
/// Maps PortWind station metadata and observations into shared persistence models.
/// </summary>
public class PortWindMappingService : IPortWindMapping
{
    private static readonly Regex MultiWhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly char[] MojibakeIndicators = ['Ã', 'Â', 'Ð', '�'];

    public PortWindStationRefreshResult MapToStationRefreshResult(IReadOnlyDictionary<string, PortWindStationCatalogEntry> stations)
    {
        var weatherStations = new List<WeatherStation>();
        var seenStationIds = new List<string>();
        var activeStationIds = new List<string>();
        var inactiveStationIds = new List<string>();

        foreach (var (stationId, station) in stations)
        {
            if (!TryMapWeatherStation(stationId, station, out var weatherStation))
            {
                continue;
            }

            weatherStations.Add(weatherStation);
            seenStationIds.Add(stationId);

            if (weatherStation.IsActive)
            {
                activeStationIds.Add(stationId);
            }
            else
            {
                inactiveStationIds.Add(stationId);
            }
        }

        return new PortWindStationRefreshResult
        {
            WeatherStations = weatherStations,
            SeenStationIds = seenStationIds,
            ActiveStationIds = activeStationIds,
            InactiveStationIds = inactiveStationIds
        };
    }

    public StationData? MapToStationData(string stationId, PortWindLatestResponse response)
    {
        if (string.IsNullOrWhiteSpace(stationId))
        {
            throw new ArgumentException("Station ID cannot be null or empty", nameof(stationId));
        }

        if (response.LastMeasurement is null)
        {
            return null;
        }

        var latestData = response.Data.FirstOrDefault();
        if (latestData?.WindSpeedAverage is null || latestData.WindDirectionAverage is null)
        {
            return null;
        }

        var direction = (int)Math.Round(latestData.WindDirectionAverage.Value);
        direction = ((direction % 360) + 360) % 360;

        return new StationData
        {
            StationId = stationId,
            WindSpeed = latestData.WindSpeedAverage.Value,
            WindGust = latestData.WindGust ?? latestData.WindSpeedMax,
            WindMinSpeed = null,
            Direction = direction,
            Temperature = latestData.TemperatureAverage,
            UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(response.LastMeasurement.Value).UtcDateTime,
            IsCompressed = false
        };
    }

    private static bool TryMapWeatherStation(string stationId, PortWindStationCatalogEntry station, out WeatherStation weatherStation)
    {
        weatherStation = null!;

        if (string.IsNullOrWhiteSpace(stationId) || station.Location?.Latitude is null || station.Location.Longitude is null)
        {
            return false;
        }

        var latitude = station.Location.Latitude.Value;
        var longitude = station.Location.Longitude.Value;
        if (latitude == 0 || longitude == 0 || latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
        {
            return false;
        }

        weatherStation = new WeatherStation
        {
            StationId = stationId,
            Name = NormalizeLabel(station.Label, stationId),
            Latitude = Math.Round(latitude, 5),
            Longitude = Math.Round(longitude, 5),
            Altitude = 0,
            Country = null,
            IsActive = station.Status == true && station.History == true,
            Provider = PortWindOptions.ProviderName,
            IsMain = false
        };

        return true;
    }

    private static string NormalizeLabel(string? label, string stationId)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return stationId;
        }

        var normalized = MultiWhitespaceRegex.Replace(label, " ").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return stationId;
        }

        if (!ContainsMojibake(normalized))
        {
            return normalized;
        }

        return TryRepairUtf8Mojibake(normalized);
    }

    private static bool ContainsMojibake(string value)
    {
        return value.IndexOfAny(MojibakeIndicators) >= 0;
    }

    private static string TryRepairUtf8Mojibake(string value)
    {
        try
        {
            var repaired = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(value));
            if (string.IsNullOrWhiteSpace(repaired) || repaired.Contains('�'))
            {
                return value;
            }

            return CountMojibakeIndicators(repaired) < CountMojibakeIndicators(value)
                ? repaired
                : value;
        }
        catch
        {
            return value;
        }
    }

    private static int CountMojibakeIndicators(string value)
    {
        return value.Count(character => MojibakeIndicators.Contains(character));
    }
}

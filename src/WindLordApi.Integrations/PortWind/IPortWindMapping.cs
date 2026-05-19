using WindLordApi.Data.Models;

namespace WindLordApi.Integrations.PortWind;

public interface IPortWindMapping
{
    List<WeatherStation> MapStations(IReadOnlyDictionary<string, PortWindStationDto> stations);
    List<StationData> MapObservations(string stationId, IReadOnlyList<PortWindObservationDto> observations);
    string NormalizeStationLabel(string label);
}
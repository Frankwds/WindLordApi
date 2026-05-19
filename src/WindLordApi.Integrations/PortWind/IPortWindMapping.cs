using WindLordApi.Data.Models;

namespace WindLordApi.Integrations.PortWind;

/// <summary>
/// Maps PortWind payloads into shared persistence models.
/// </summary>
public interface IPortWindMapping
{
    /// <summary>
    /// Maps the PortWind station catalog into weather station records and active-state groups.
    /// </summary>
    PortWindStationRefreshResult MapToStationRefreshResult(IReadOnlyDictionary<string, PortWindStationCatalogEntry> stations);

    /// <summary>
    /// Maps the PortWind latest observation payload to a shared StationData record.
    /// </summary>
    StationData? MapToStationData(string stationId, PortWindLatestResponse response);
}
using System.Text.Json;

namespace WindLordApi.Integrations.MetFrost;

public interface IMetFrostClient
{
    Task<JsonDocument> FetchMetStationDataAsync(string[] stationIds, CancellationToken cancellationToken = default);
}
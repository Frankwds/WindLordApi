using System.Text.Json;

namespace WindLordApi.Integrations.Clients.MetFrost;

public interface IMetFrostClient
{
    Task<JsonDocument> FetchMetStationDataAsync(string[] stationIds, CancellationToken cancellationToken = default);
}
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindLordApi.Integrations.PortWind;

public sealed class PortWindStationDto
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> Properties { get; init; } = [];
}

public sealed class PortWindObservationResponseDto
{
    [JsonPropertyName("data")]
    public List<PortWindObservationDto> Data { get; init; } = [];
}

public sealed class PortWindObservationDto
{
    [JsonPropertyName("uts")]
    public long? Uts { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Properties { get; init; } = [];
}
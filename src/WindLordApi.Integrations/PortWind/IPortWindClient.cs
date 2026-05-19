namespace WindLordApi.Integrations.PortWind;

public interface IPortWindClient
{
    Task<IReadOnlyDictionary<string, PortWindStationDto>> FetchStationsAsync(CancellationToken cancellationToken = default);
    Task<PortWindObservationResponseDto> FetchLatestAndPreviousObservationAsync(string stationId, CancellationToken cancellationToken = default);
}
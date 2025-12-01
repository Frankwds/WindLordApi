namespace WindLordApi.Data.Services;

public interface IWeatherStationService
{
    Task<IEnumerable<string>> GetActiveMETStationIdsAsync(CancellationToken cancellationToken = default);
}


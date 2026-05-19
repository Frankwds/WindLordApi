using Microsoft.Extensions.Logging;
using WindLordApi.Data.Services;
using WindLordApi.Integrations.PortWind;

namespace WindLordApi.Worker.Services;

public class PortWindStationRefreshService : IPortWindStationRefreshService
{
    private readonly IPortWindClient _portWindClient;
    private readonly IPortWindMapping _portWindMapping;
    private readonly IWeatherStationService _weatherStationService;
    private readonly ILogger<PortWindStationRefreshService> _logger;

    public PortWindStationRefreshService(
        IPortWindClient portWindClient,
        IPortWindMapping portWindMapping,
        IWeatherStationService weatherStationService,
        ILogger<PortWindStationRefreshService> logger)
    {
        _portWindClient = portWindClient;
        _portWindMapping = portWindMapping;
        _weatherStationService = weatherStationService;
        _logger = logger;
    }

    public async Task<int> SyncWeatherStationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("PortWind: Fetching station list...");
            var providerStations = await _portWindClient.FetchStationsAsync(cancellationToken);
            var weatherStations = _portWindMapping.MapStations(providerStations);
            var currentStationIds = weatherStations
                .Select(weatherStation => weatherStation.StationId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var activeStationIds = weatherStations
                .Where(weatherStation => weatherStation.IsActive)
                .Select(weatherStation => weatherStation.StationId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (weatherStations.Count > 0)
            {
                await _weatherStationService.UpsertManyAsync(weatherStations.ToArray(), cancellationToken);

                if (activeStationIds.Length > 0)
                {
                    await _weatherStationService.SetStationsActiveByProviderAsync(PortWindOptions.ProviderName, activeStationIds, cancellationToken);
                }
            }

            var deactivatedCount = await _weatherStationService.SetStationsInactiveByProviderExceptAsync(
                PortWindOptions.ProviderName,
                activeStationIds,
                cancellationToken);

            _logger.LogInformation(
                "PortWind: Completed station refresh. Provider stations: {StationCount}, Active stations: {ActiveStationCount}, Deactivated stations: {DeactivatedCount}",
                currentStationIds.Length,
                activeStationIds.Length,
                deactivatedCount);

            return currentStationIds.Length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PortWind: Error refreshing weather stations");
            throw;
        }
    }
}
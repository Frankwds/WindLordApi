using WindLordApi.Data.Services;
using WindLordApi.Integrations.PortWind;

namespace WindLordApi.Worker.Services;

/// <summary>
/// Refreshes PortWind weather station metadata and provider-scoped active state.
/// </summary>
public class PortWindStationRefreshService : IPortWindStationRefreshService
{
    private const string Provider = "PortWind";
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
        _logger.LogInformation("PortWind: Refreshing weather stations...");
        var rawStations = await _portWindClient.FetchStationsAsync(cancellationToken);
        var refreshResult = _portWindMapping.MapToStationRefreshResult(rawStations);

        if (refreshResult.WeatherStations.Count > 0)
        {
            await _weatherStationService.UpsertManyAsync(refreshResult.WeatherStations.ToArray(), cancellationToken);
        }

        var activatedCount = await _weatherStationService.SetStationsActiveByProviderAsync(Provider, refreshResult.ActiveStationIds, cancellationToken);
        var inactivatedSeenCount = await _weatherStationService.SetStationsInactiveByProviderAsync(Provider, refreshResult.InactiveStationIds, cancellationToken);
        var missingCount = await _weatherStationService.SetMissingStationsInactiveByProviderAsync(Provider, refreshResult.SeenStationIds, cancellationToken);

        _logger.LogInformation(
            "PortWind: Refreshed {StationCount} station(s). Status updates: {Activated} activated, {InactiveSeen} explicitly inactive, {MissingInactive} missing stations inactive",
            refreshResult.WeatherStations.Count,
            activatedCount,
            inactivatedSeenCount,
            missingCount);

        return refreshResult.WeatherStations.Count;
    }
}

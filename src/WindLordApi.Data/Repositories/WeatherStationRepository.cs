using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FlexLabs.EntityFrameworkCore.Upsert;
using WindLordApi.Data.Models;

namespace WindLordApi.Data.Repositories;

/// <summary>
/// Repository implementation for WeatherStation entity.
/// </summary>
public class WeatherStationRepository : Repository<WeatherStation>, IWeatherStationRepository
{
    private readonly ILogger<WeatherStationRepository> _logger;

    public WeatherStationRepository(ApplicationDbContext context, ILogger<WeatherStationRepository> logger)
        : base(context)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<string>> GetActiveStationIdsByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        var stationIds = await _dbSet
            .Where(ws => ws.IsActive)
            .Where(ws => ws.Provider == provider)
            .Select(ws => ws.StationId)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} active station IDs for provider {Provider}", stationIds.Count, provider);
        return stationIds;
    }

    public async Task<IEnumerable<string>> GetInactiveStationIdsByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        var stationIds = await _dbSet
            .Where(ws => !ws.IsActive)
            .Where(ws => ws.Provider == provider)
            .Select(ws => ws.StationId)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} inactive station IDs for provider {Provider}", stationIds.Count, provider);
        return stationIds;
    }

    public async Task<int> SetAllStationsWithDataToActiveByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        var updated = await _dbSet
            .Where(ws => !ws.IsActive)
            .Where(ws => ws.Provider == provider)
            .Where(ws => _context.Set<StationData>().Any(sd => sd.StationId == ws.StationId))
            .ExecuteUpdateAsync(
                setter => setter.SetProperty(ws => ws.IsActive, true),
                cancellationToken);

        _logger.LogDebug("Set {Count} stations to active for provider {Provider} based on persisted data", updated, provider);
        return updated;
    }

    public async Task<int> SetAllStationsWithoutDataToInactiveByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        var updated = await _dbSet
            .Where(ws => ws.IsActive)
            .Where(ws => ws.Provider == provider)
            .Where(ws => !_context.Set<StationData>().Any(sd => sd.StationId == ws.StationId))
            .ExecuteUpdateAsync(
                setter => setter.SetProperty(ws => ws.IsActive, false),
                cancellationToken);

        _logger.LogDebug("Set {Count} stations to inactive for provider {Provider} based on missing persisted data", updated, provider);
        return updated;
    }

    public async Task<int> SetStationsActiveByProviderAsync(string provider, IEnumerable<string> stationIds, CancellationToken cancellationToken = default)
    {
        var stationIdList = stationIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (stationIdList.Count == 0)
        {
            return 0;
        }

        var updated = await _dbSet
            .Where(ws => !ws.IsActive)
            .Where(ws => ws.Provider == provider)
            .Where(ws => stationIdList.Contains(ws.StationId))
            .ExecuteUpdateAsync(
                setter => setter.SetProperty(ws => ws.IsActive, true),
                cancellationToken);

        _logger.LogDebug("Set {Count} station(s) to active for provider {Provider}", updated, provider);
        return updated;
    }

    public async Task<int> SetStationsInactiveByProviderAsync(string provider, IEnumerable<string> stationIds, CancellationToken cancellationToken = default)
    {
        var stationIdList = stationIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (stationIdList.Count == 0)
        {
            return 0;
        }

        var updated = await _dbSet
            .Where(ws => ws.IsActive)
            .Where(ws => ws.Provider == provider)
            .Where(ws => stationIdList.Contains(ws.StationId))
            .ExecuteUpdateAsync(
                setter => setter.SetProperty(ws => ws.IsActive, false),
                cancellationToken);

        _logger.LogDebug("Set {Count} station(s) to inactive for provider {Provider}", updated, provider);
        return updated;
    }

    public async Task<int> SetMissingStationsInactiveByProviderAsync(string provider, IEnumerable<string> seenStationIds, CancellationToken cancellationToken = default)
    {
        var seenStationIdList = seenStationIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();

        var query = _dbSet
            .Where(ws => ws.IsActive)
            .Where(ws => ws.Provider == provider);

        if (seenStationIdList.Count > 0)
        {
            query = query.Where(ws => !seenStationIdList.Contains(ws.StationId));
        }

        var updated = await query.ExecuteUpdateAsync(
            setter => setter.SetProperty(ws => ws.IsActive, false),
            cancellationToken);

        _logger.LogDebug("Set {Count} missing station(s) to inactive for provider {Provider}", updated, provider);
        return updated;
    }

    public async Task<List<WeatherStation>> GetStationsWithMissingCountryAsync(CancellationToken cancellationToken = default)
    {
        var stations = await _dbSet
            .Where(ws => ws.Country == null || ws.Country == "UKJENT")
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} weather stations with missing country", stations.Count);
        return stations;
    }

    public async Task<int> UpsertRangeAsync(IEnumerable<WeatherStation> entities, CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();
        if (entitiesList.Count == 0) return 0;

        // Use FlexLabs upsert: ON CONFLICT (station_id) DO UPDATE
        // This is type-safe and eliminates SQL injection risks
        return await _context.UpsertRange<WeatherStation>(entitiesList)
            .On(ws => ws.StationId)
            .WhenMatched((existing, incoming) => new WeatherStation
            {
                Name = incoming.Name,
                Latitude = incoming.Latitude,
                Longitude = incoming.Longitude,
                Altitude = incoming.Altitude,
                Provider = incoming.Provider,
                UpdatedAt = incoming.UpdatedAt,
                // Holfuy stations seen in sync should always be active.
                IsActive = incoming.Provider == "Holfuy" ? true : existing.IsActive
                // Country and IsMain are intentionally excluded - managed by CountryLocatorService
                // MET is_active is intentionally managed by MetFrost active status sync job
            })
            .RunAsync(cancellationToken);
    }

    public async Task<int> UpdateCountriesAsync(IEnumerable<WeatherStation> entities, CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();
        if (entitiesList.Count == 0) return 0;

        // Use FlexLabs upsert: ON CONFLICT (station_id) DO UPDATE
        // Only updates Country and IsMain - used by CountryLocatorService
        return await _context.UpsertRange<WeatherStation>(entitiesList)
            .On(ws => ws.StationId)
            .WhenMatched((existing, incoming) => new WeatherStation
            {
                Country = incoming.Country,
                IsMain = incoming.IsMain
            })
            .RunAsync(cancellationToken);
    }
}


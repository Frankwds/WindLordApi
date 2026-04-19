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

    public async Task<IEnumerable<string>> GetActiveMETStationIdsAsync(CancellationToken cancellationToken = default)
    {
        var stationIds = await _dbSet
            .Where(ws => ws.IsActive)
            .Where(ws => ws.Provider == "MET")
            .Select(ws => ws.StationId)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("MetFrost: Retrieved {Count} active station IDs", stationIds.Count);
        return stationIds;
    }

    public async Task<IEnumerable<string>> GetInactiveMETStationIdsAsync(CancellationToken cancellationToken = default)
    {
        var stationIds = await _dbSet
            .Where(ws => !ws.IsActive)
            .Where(ws => ws.Provider == "MET")
            .Select(ws => ws.StationId)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("MetFrost: Retrieved {Count} inactive station IDs", stationIds.Count);
        return stationIds;
    }

    public async Task<int> SetAllStationsWithDataToActiveAsync(CancellationToken cancellationToken = default)
    {
        // Use ExecuteUpdateAsync for type-safe bulk update without loading entities
        // Only update stations that are currently inactive (is_active = false)
        // This ensures we only count actual changes
        var updated = await _dbSet
            .Where(ws => !ws.IsActive)
            .Where(ws => ws.Provider == "MET")
            .Where(ws => _context.Set<StationData>().Any(sd => sd.StationId == ws.StationId))
            .ExecuteUpdateAsync(
                setter => setter.SetProperty(ws => ws.IsActive, true),
                cancellationToken);

        _logger.LogDebug("MetFrost: Set {Count} stations to active (stations with data)", updated);
        return updated;
    }

    public async Task<int> SetAllStationsWithoutDataToInactiveAsync(CancellationToken cancellationToken = default)
    {
        // Use ExecuteUpdateAsync for type-safe bulk update without loading entities
        // Only update stations that are currently active (is_active = true)
        // This ensures we only count actual changes
        var updated = await _dbSet
            .Where(ws => ws.IsActive)
            .Where(ws => ws.Provider == "MET")
            .Where(ws => !_context.Set<StationData>().Any(sd => sd.StationId == ws.StationId))
            .ExecuteUpdateAsync(
                setter => setter.SetProperty(ws => ws.IsActive, false),
                cancellationToken);

        _logger.LogDebug("MetFrost: Set {Count} stations to inactive (stations without data)", updated);
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


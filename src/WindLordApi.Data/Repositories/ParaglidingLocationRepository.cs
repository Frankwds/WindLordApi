using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FlexLabs.EntityFrameworkCore.Upsert;
using WindLordApi.Data.Models;

namespace WindLordApi.Data.Repositories;

/// <summary>
/// Repository implementation for ParaglidingLocation entity.
/// </summary>
public class ParaglidingLocationRepository : Repository<ParaglidingLocation>, IParaglidingLocationRepository
{
    private readonly ILogger<ParaglidingLocationRepository> _logger;

    public ParaglidingLocationRepository(ApplicationDbContext context, ILogger<ParaglidingLocationRepository> logger)
        : base(context)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<LocationsWithOldestForecast>> GetLocationsWithOldestForecastAsync(int limit, CancellationToken cancellationToken = default)
    {
        var locations = await _context.LocationsWithOldestForecast
            .OrderBy(l => l.OldestUpdatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} locations with oldest forecast from view (limit: {Limit})", locations.Count, limit);
        return locations;
    }

    public async Task<IEnumerable<LocationsWithoutForecast>> GetLocationsWithoutForecastAsync(int limit, CancellationToken cancellationToken = default)
    {
        var locations = await _context.LocationsWithoutForecast
            .OrderBy(l => l.Name)
            .Take(limit)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} locations without forecast from view (limit: {Limit})", locations.Count, limit);
        return locations;
    }

    public async Task<IEnumerable<Guid>> GetOpenMeteoRefreshCandidatesAsync(int limit, CancellationToken cancellationToken = default)
    {
        var locationIds = await _dbSet
            .Where(pl => pl.IsActive && pl.IsMain)
            .Select(pl => new
            {
                pl.Id,
                LatestOpenMeteoForecastTime = _context.ForecastCaches
                    .Where(fc => fc.LocationId == pl.Id && !fc.IsYrData)
                    .Max(fc => (DateTime?)fc.Time)
            })
            .OrderBy(location => location.LatestOpenMeteoForecastTime.HasValue)
            .ThenBy(location => location.LatestOpenMeteoForecastTime)
            .ThenBy(location => location.Id)
            .Take(limit)
            .Select(location => location.Id)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} locations with the shortest Open-Meteo forecast tail (limit: {Limit})", locationIds.Count, limit);
        return locationIds;
    }

    public async Task<IEnumerable<ParaglidingLocation>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idsList = ids.ToList();
        if (idsList.Count == 0)
        {
            return Enumerable.Empty<ParaglidingLocation>();
        }

        var locations = await _dbSet
            .Where(pl => idsList.Contains(pl.Id) && pl.IsActive && pl.IsMain)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} active main locations by IDs", locations.Count);
        return locations;
    }
}


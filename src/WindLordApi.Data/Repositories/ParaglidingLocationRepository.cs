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

    public async Task<IEnumerable<Guid>> GetMetYrRefreshCandidatesAsync(int limit, CancellationToken cancellationToken = default)
    {
        var locationsWithoutForecast = await _dbSet
            .Where(location => location.IsActive && location.IsMain)
            .Where(location => !_context.ForecastCaches.Any(forecast => forecast.LocationId == location.Id))
            .OrderBy(location => location.Name)
            .Select(location => location.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var remainingSlots = limit - locationsWithoutForecast.Count;
        if (remainingSlots <= 0)
        {
            _logger.LogDebug("Retrieved {Count} MetYr refresh candidates without existing forecast coverage (limit: {Limit})", locationsWithoutForecast.Count, limit);
            return locationsWithoutForecast;
        }

        var locationsWithOldestForecast = await _dbSet
            .Where(location => location.IsActive && location.IsMain)
            .Where(location => !locationsWithoutForecast.Contains(location.Id))
            .Select(location => new
            {
                location.Id,
                OldestUpdatedAt = _context.ForecastCaches
                    .Where(forecast => forecast.LocationId == location.Id)
                    .Min(forecast => (DateTime?)forecast.UpdatedAt)
            })
            .Where(location => location.OldestUpdatedAt.HasValue)
            .OrderBy(location => location.OldestUpdatedAt)
            .ThenBy(location => location.Id)
            .Take(remainingSlots)
            .Select(location => location.Id)
            .ToListAsync(cancellationToken);

        var locationIds = locationsWithoutForecast
            .Concat(locationsWithOldestForecast)
            .ToList();

        _logger.LogDebug("Retrieved {Count} MetYr refresh candidates prioritized by missing then oldest forecast coverage (limit: {Limit})", locationIds.Count, limit);
        return locationIds;
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


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

    public async Task<IEnumerable<LocationsWithOldestForecast>> GetLocationsWithOldestForecastAsync(CancellationToken cancellationToken = default)
    {
        var locations = await _context.LocationsWithOldestForecast
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} locations with oldest forecast from view", locations.Count);
        return locations;
    }

    public async Task<IEnumerable<LocationsWithoutForecast>> GetLocationsWithoutForecastAsync(CancellationToken cancellationToken = default)
    {
        var locations = await _context.LocationsWithoutForecast
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} locations without forecast from view", locations.Count);
        return locations;
    }
}


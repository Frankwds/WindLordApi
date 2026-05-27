using Microsoft.EntityFrameworkCore;
using FlexLabs.EntityFrameworkCore.Upsert;
using WindLordApi.Data.Models;

namespace WindLordApi.Data.Repositories;

/// <summary>
/// Repository implementation for StationData entity.
/// </summary>
public class StationDataRepository : Repository<StationData>, IStationDataRepository
{
    public StationDataRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<int> UpsertRangeAsync(IEnumerable<StationData> entities, CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();
        if (entitiesList.Count == 0) return 0;

        // Use FlexLabs upsert: ON CONFLICT (station_id, updated_at) DO NOTHING
        // This is type-safe and eliminates SQL injection risks
        return await _context.UpsertRange<StationData>(entitiesList)
            .On(sd => new { sd.StationId, sd.UpdatedAt })
            .NoUpdate()
            .RunAsync(cancellationToken);
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoffTime, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sd => sd.UpdatedAt < cutoffTime)
            .ExecuteDeleteAsync(cancellationToken);
    }
}


using Microsoft.EntityFrameworkCore;
using FlexLabs.EntityFrameworkCore.Upsert;
using WindLordApi.Data.Models;

namespace WindLordApi.Data.Repositories;

/// <summary>
/// Repository implementation for LatestStationData entity.
/// </summary>
public class LatestStationDataRepository : Repository<LatestStationData>, ILatestStationDataRepository
{
    public LatestStationDataRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<int> UpsertRangeAsync(IEnumerable<LatestStationData> entities, CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();
        if (entitiesList.Count == 0) return 0;

        // Use FlexLabs upsert: ON CONFLICT (station_id) DO UPDATE
        // This is type-safe and eliminates SQL injection risks
        return await _context.UpsertRange<LatestStationData>(entitiesList)
            .On(lsd => lsd.StationId)
            .WhenMatched((existing, incoming) => new LatestStationData
            {
                // Id is not set - primary key is preserved automatically
                StationId = incoming.StationId,
                WindSpeed = incoming.WindSpeed,
                WindGust = incoming.WindGust,
                Direction = incoming.Direction,
                Temperature = incoming.Temperature,
                UpdatedAt = incoming.UpdatedAt,
                WindMinSpeed = incoming.WindMinSpeed
            })
            .RunAsync(cancellationToken);
    }
}


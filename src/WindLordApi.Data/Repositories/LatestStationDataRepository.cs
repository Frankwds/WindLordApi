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
}


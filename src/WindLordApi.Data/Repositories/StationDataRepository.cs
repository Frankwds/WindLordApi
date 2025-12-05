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
}


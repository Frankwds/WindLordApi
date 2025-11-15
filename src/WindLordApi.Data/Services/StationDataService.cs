using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WindLordApi.Data.Models;

namespace WindLordApi.Data.Services;

public class StationDataService : IStationDataService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<StationDataService> _logger;

    public StationDataService(ApplicationDbContext dbContext, ILogger<StationDataService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IEnumerable<StationData>> GetByStationIdAsync(string stationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stationId))
        {
            throw new ArgumentException("Station ID cannot be null or empty", nameof(stationId));
        }

        var stationData = await _dbContext.StationData
            .Where(sd => sd.StationId == stationId)
            .OrderByDescending(sd => sd.UpdatedAt)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} records for station {StationId}", stationData.Count, stationId);

        return stationData;
    }


}

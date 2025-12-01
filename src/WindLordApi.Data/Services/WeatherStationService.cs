using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace WindLordApi.Data.Services;

public class WeatherStationService : IWeatherStationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<WeatherStationService> _logger;

    public WeatherStationService(
        ApplicationDbContext dbContext,
        ILogger<WeatherStationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> GetActiveMETStationIdsAsync(CancellationToken cancellationToken = default)
    {
        var stationIds = await _dbContext.WeatherStations
            .Where(ws => ws.IsActive)
            .Where(ws => ws.Provider == "MET")
            .Select(ws => ws.StationId)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} active station IDs", stationIds.Count);
        return stationIds;
    }
}


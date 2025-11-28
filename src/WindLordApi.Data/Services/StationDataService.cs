using EFCore.BulkExtensions;
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

    public async Task UpsertManyAsync(StationData[] stationDataArray, CancellationToken cancellationToken = default)
    {
        if (stationDataArray == null || stationDataArray.Length == 0)
        {
            throw new ArgumentException("Station data array cannot be null or empty", nameof(stationDataArray));
        }
        var records = stationDataArray.Where(sd => sd is not null).ToList();
        if (records.Count == 0)
        {
            _logger.LogWarning("Upsert skipped because all entries were null");
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {

            var bulkConfig = new BulkConfig
            {
                UpdateByProperties = new List<string> { nameof(StationData.StationId), nameof(StationData.UpdatedAt) },
                PropertiesToIncludeOnUpdate = new List<string>() // empty list => skip updates, i.e. insert-only
            };

            await _dbContext.BulkInsertOrUpdateAsync(records, bulkConfig, cancellationToken: cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Upserted {Count} station data rows (conflicts ignored)", records.Count);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to upsert station data batch");
            throw;
        }
    }
}

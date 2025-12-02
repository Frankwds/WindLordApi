using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FlexLabs.EntityFrameworkCore.Upsert;
using WindLordApi.Data.Models;

namespace WindLordApi.Data.Services;

public class StationDataService : IStationDataService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<StationDataService> _logger;
    private const int BatchSize = 1000; // Process in batches to avoid parameter limits

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

    public async Task<int> UpsertManyAsync(StationData[] stationDataArray, CancellationToken cancellationToken = default)
    {
        if (stationDataArray == null || stationDataArray.Length == 0)
        {
            throw new ArgumentException("Station data array cannot be null or empty", nameof(stationDataArray));
        }
        var records = stationDataArray.Where(sd => sd is not null).ToList();
        if (records.Count == 0)
        {
            throw new ArgumentException("Station data array cannot contain only null elements", nameof(stationDataArray));
        }

        var totalInserted = 0;

        // Process in batches to avoid parameter limits
        for (int i = 0; i < records.Count; i += BatchSize)
        {
            var batch = records.Skip(i).Take(BatchSize).ToList();
            totalInserted += await UpsertBatchAsync(batch, cancellationToken);
        }

        return totalInserted;
    }

    private async Task<int> UpsertBatchAsync(List<StationData> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return 0;

        // Use explicit transaction for Supabase connection pooler compatibility
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Use FlexLabs upsert: ON CONFLICT (station_id, updated_at) DO NOTHING
            // This is type-safe and eliminates SQL injection risks
            var insertedCount = await _dbContext.UpsertRange<StationData>(batch)
                .On(sd => new { sd.StationId, sd.UpdatedAt })
                .NoUpdate()
                .RunAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return insertedCount;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to upsert station data batch of {Count} records", batch.Count);
            throw;
        }
    }
}
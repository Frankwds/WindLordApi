using Microsoft.Extensions.Logging;
using WindLordApi.Data.Models;
using WindLordApi.Data.Repositories;

namespace WindLordApi.Data.Services;

public class StationDataService : IStationDataService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StationDataService> _logger;
    private const int BatchSize = 1000; // Process in batches to avoid parameter limits

    public StationDataService(IUnitOfWork unitOfWork, ILogger<StationDataService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
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
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var insertedCount = await _unitOfWork.StationData.UpsertRangeAsync(batch, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            return insertedCount;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            _logger.LogError(ex, "Failed to upsert station data batch of {Count} records", batch.Count);
            throw;
        }
    }
}
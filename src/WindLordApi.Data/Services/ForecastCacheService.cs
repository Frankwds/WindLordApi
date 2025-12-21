using Microsoft.Extensions.Logging;
using WindLordApi.Data.Models;
using WindLordApi.Data.Repositories;

namespace WindLordApi.Data.Services;

/// <summary>
/// Service implementation for ForecastCache entity operations.
/// </summary>
public class ForecastCacheService : IForecastCacheService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ForecastCacheService> _logger;
    private const int BatchSize = 1000; // Process in batches to avoid parameter limits

    public ForecastCacheService(
        IUnitOfWork unitOfWork,
        ILogger<ForecastCacheService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> UpsertManyAsync(ForecastCache[] forecasts, CancellationToken cancellationToken = default)
    {
        if (forecasts == null || forecasts.Length == 0)
        {
            throw new ArgumentException("Forecasts array cannot be null or empty", nameof(forecasts));
        }

        var records = forecasts.Where(fc => fc is not null).ToList();
        if (records.Count == 0)
        {
            throw new ArgumentException("Forecasts array cannot contain only null elements", nameof(forecasts));
        }

        var totalAffected = 0;

        // Process in batches to avoid parameter limits
        for (int i = 0; i < records.Count; i += BatchSize)
        {
            var batch = records.Skip(i).Take(BatchSize).ToList();
            totalAffected += await UpsertBatchAsync(batch, cancellationToken);
        }

        return totalAffected;
    }

    private async Task<int> UpsertBatchAsync(List<ForecastCache> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return 0;

        // Use explicit transaction for Supabase connection pooler compatibility
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var insertedOrUpdatedCount = await _unitOfWork.ForecastCaches.UpsertRangeAsync(batch, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            return insertedOrUpdatedCount;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            _logger.LogError(ex, "Failed to upsert forecast cache batch of {Count} records", batch.Count);
            throw;
        }
    }

    public async Task<int> DeleteOldForecastsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting forecast data older than: {CutoffTime}", cutoffTime);

        var deletedCount = await _unitOfWork.ForecastCaches.DeleteOldForecastsAsync(cutoffTime, cancellationToken);

        _logger.LogInformation("Forecast data cleanup completed successfully. Deleted {Count} records", deletedCount);

        return deletedCount;
    }
}


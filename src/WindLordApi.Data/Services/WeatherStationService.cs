using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WindLordApi.Data.Models;
using WindLordApi.Data.Extensions;

namespace WindLordApi.Data.Services;

public class WeatherStationService : IWeatherStationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<WeatherStationService> _logger;
    private const int BatchSize = 1000; // Process in batches to avoid parameter limits

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

    public async Task<IEnumerable<string>> GetInactiveMETStationIdsAsync(CancellationToken cancellationToken = default)
    {
        var stationIds = await _dbContext.WeatherStations
            .Where(ws => !ws.IsActive)
            .Where(ws => ws.Provider == "MET")
            .Select(ws => ws.StationId)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} inactive station IDs", stationIds.Count);
        return stationIds;
    }

    public async Task<int> UpsertManyAsync(WeatherStation[] weatherStations, CancellationToken cancellationToken = default)
    {
        if (weatherStations == null || weatherStations.Length == 0)
        {
            throw new ArgumentException("Weather stations array cannot be null or empty", nameof(weatherStations));
        }
        var records = weatherStations.Where(ws => ws is not null).ToList();
        if (records.Count == 0)
        {
            throw new ArgumentException("Weather stations array cannot contain only null elements", nameof(weatherStations));
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

    private async Task<int> UpsertBatchAsync(List<WeatherStation> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return 0;

        const int maxAttempts = 2; // Original attempt + 1 retry
        const int retryDelayMs = 3000; // 3 seconds

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                // Use explicit transaction for Supabase connection pooler compatibility
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var insertedOrUpdatedCount = await _dbContext.UpsertRange<WeatherStation>(batch)
                        .On(ws => ws.StationId)
                        .WhenMatched((existing, incoming) => new WeatherStation
                        {
                            Name = incoming.Name,
                            Latitude = incoming.Latitude,
                            Longitude = incoming.Longitude,
                            Altitude = incoming.Altitude,
                            Country = incoming.Country,
                            Provider = incoming.Provider,
                            UpdatedAt = incoming.UpdatedAt,
                            IsMain = incoming.IsMain
                            // is_active is intentionally excluded - managed separately
                        })
                        .RunAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    if (attempt > 1)
                    {
                        _logger.LogInformation("Successfully upserted weather stations batch after retry (attempt {Attempt})", attempt);
                    }

                    return insertedOrUpdatedCount;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    // Only retry on transient errors and if we have attempts left
                    if (RetryExtensions.IsRetryableError(ex) && attempt < maxAttempts)
                    {
                        _logger.LogWarning(ex,
                            "Transient error on attempt {Attempt}/{MaxAttempts} for batch of {Count} records. Retrying after {Delay}ms...",
                            attempt, maxAttempts, batch.Count, retryDelayMs);

                        await Task.Delay(retryDelayMs, cancellationToken);
                        continue; // Retry
                    }

                    // Not retryable or out of attempts - throw
                    _logger.LogError(ex, "Failed to upsert weather stations batch of {Count} records after {Attempt} attempt(s)",
                        batch.Count, attempt);
                    throw;
                }
            }
            catch (Exception ex)
            {
                // Handle errors from BeginTransactionAsync or other operations outside the transaction
                // Only retry on transient errors and if we have attempts left
                if (RetryExtensions.IsRetryableError(ex) && attempt < maxAttempts)
                {
                    _logger.LogWarning(ex,
                        "Transient error during transaction setup on attempt {Attempt}/{MaxAttempts} for batch of {Count} records. Retrying after {Delay}ms...",
                        attempt, maxAttempts, batch.Count, retryDelayMs);

                    await Task.Delay(retryDelayMs, cancellationToken);
                    continue; // Retry
                }

                // Final attempt failed or non-retryable error
                _logger.LogError(ex, "Failed to upsert weather stations batch of {Count} records after {Attempt} attempt(s)",
                    batch.Count, attempt);
                throw;
            }
        }

        // Should never reach here, but compiler needs it
        throw new InvalidOperationException("Unexpected retry loop exit");
    }

    public async Task<int> SetActiveStationsWithDataAsync(CancellationToken cancellationToken = default)
    {
        // Use a direct SQL query with EXISTS to find stations with data
        // Only update stations that are currently inactive (is_active = false)
        // This ensures we only count actual changes
        var sql = @"
            UPDATE weather_stations ws
            SET is_active = true
            WHERE EXISTS (
                SELECT 1 
                FROM station_data sd 
                WHERE sd.station_id = ws.station_id
            )
            AND is_active = false
            AND provider = 'MET'";

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var updated = await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Set {Count} stations to active (stations with data)", updated);
            return updated;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to set active stations with data");
            throw;
        }
    }

    public async Task<int> SetInactiveStationsWithoutDataAsync(CancellationToken cancellationToken = default)
    {
        // Use a direct SQL query with NOT EXISTS to find stations without data
        // Only update stations that are currently active (is_active = true)
        // This ensures we only count actual changes
        var sql = @"
            UPDATE weather_stations ws
            SET is_active = false
            WHERE NOT EXISTS (
                SELECT 1 
                FROM station_data sd 
                WHERE sd.station_id = ws.station_id
            )
            AND is_active = true
            AND provider = 'MET'";

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var updated = await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Set {Count} stations to inactive (stations without data)", updated);
            return updated;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to set inactive stations without data");
            throw;
        }
    }

}


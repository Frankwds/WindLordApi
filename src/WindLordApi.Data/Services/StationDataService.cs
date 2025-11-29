using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
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

        _logger.LogInformation("Upserting {Count} station data rows", records.Count);

        // Process in batches to avoid parameter limits
        for (int i = 0; i < records.Count; i += BatchSize)
        {
            var batch = records.Skip(i).Take(BatchSize).ToList();
            await UpsertBatchAsync(batch, cancellationToken);
        }

        _logger.LogInformation("Done Upserting {Count} station data rows (conflicts ignored)", records.Count);
    }

    private async Task UpsertBatchAsync(List<StationData> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return;

        // Use explicit transaction for Supabase connection pooler compatibility
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var parameters = new List<object>();
            var valueClauses = new List<string>();

            for (int i = 0; i < batch.Count; i++)
            {
                var record = batch[i];
                var paramIndex = i * 8; // 8 parameters per record

                // Generate parameter names
                var windSpeedParam = $"@p{paramIndex}";
                var windGustParam = $"@p{paramIndex + 1}";
                var windMinSpeedParam = $"@p{paramIndex + 2}";
                var directionParam = $"@p{paramIndex + 3}";
                var temperatureParam = $"@p{paramIndex + 4}";
                var updatedAtParam = $"@p{paramIndex + 5}";
                var isCompressedParam = $"@p{paramIndex + 6}";
                var stationIdParam = $"@p{paramIndex + 7}";

                // Add parameters
                parameters.Add(new NpgsqlParameter(windSpeedParam, record.WindSpeed));
                parameters.Add(new NpgsqlParameter(windGustParam, (object?)record.WindGust ?? DBNull.Value));
                parameters.Add(new NpgsqlParameter(windMinSpeedParam, (object?)record.WindMinSpeed ?? DBNull.Value));
                parameters.Add(new NpgsqlParameter(directionParam, record.Direction));
                parameters.Add(new NpgsqlParameter(temperatureParam, (object?)record.Temperature ?? DBNull.Value));
                parameters.Add(new NpgsqlParameter(updatedAtParam, record.UpdatedAt));
                parameters.Add(new NpgsqlParameter(isCompressedParam, record.IsCompressed));
                parameters.Add(new NpgsqlParameter(stationIdParam, record.StationId));

                // Build VALUES clause
                valueClauses.Add($"({windSpeedParam}, {windGustParam}, {windMinSpeedParam}, {directionParam}, {temperatureParam}, {updatedAtParam}, {isCompressedParam}, {stationIdParam})");
            }

            var sql = $@"
                INSERT INTO station_data (wind_speed, wind_gust, wind_min_speed, direction, temperature, updated_at, is_compressed, station_id)
                VALUES {string.Join(", ", valueClauses)}
                ON CONFLICT (station_id, updated_at) DO NOTHING";

            await _dbContext.Database.ExecuteSqlRawAsync(sql, parameters.ToArray(), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to upsert station data batch of {Count} records", batch.Count);
            throw;
        }
    }
}
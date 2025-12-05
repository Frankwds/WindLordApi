using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FlexLabs.EntityFrameworkCore.Upsert;
using WindLordApi.Data.Models;
using WindLordApi.Data.Repositories;

namespace WindLordApi.Data.Services;

public class WeatherStationService : IWeatherStationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WeatherStationService> _logger;
    private const int BatchSize = 1000; // Process in batches to avoid parameter limits

    public WeatherStationService(
        IUnitOfWork unitOfWork,
        ILogger<WeatherStationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> GetActiveMETStationIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.WeatherStations.GetActiveMETStationIdsAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetInactiveMETStationIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.WeatherStations.GetInactiveMETStationIdsAsync(cancellationToken);
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

        // Use explicit transaction for Supabase connection pooler compatibility
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var insertedOrUpdatedCount = await _unitOfWork.Context.UpsertRange<WeatherStation>(batch)
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

            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            return insertedOrUpdatedCount;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            _logger.LogError(ex, "Failed to upsert weather stations batch of {Count} records", batch.Count);
            throw;
        }
    }

    public async Task<int> SetActiveStationsWithDataAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.WeatherStations.SetActiveStationsWithDataAsync(cancellationToken);
    }

    public async Task<int> SetInactiveStationsWithoutDataAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.WeatherStations.SetInactiveStationsWithoutDataAsync(cancellationToken);
    }

}


using Microsoft.Extensions.Logging;
using WindLordApi.Data.Models;
using WindLordApi.Data.Repositories;

namespace WindLordApi.Data.Services;

public class WeatherStationService : IWeatherStationService
{
    private static readonly StringComparer ProviderComparer = StringComparer.Ordinal;
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

    public async Task<IEnumerable<string>> GetActiveStationIdsByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        ValidateProvider(provider);
        return await _unitOfWork.WeatherStations.GetActiveStationIdsByProviderAsync(provider, cancellationToken);
    }

    public async Task<IEnumerable<string>> GetInactiveStationIdsByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        ValidateProvider(provider);
        return await _unitOfWork.WeatherStations.GetInactiveStationIdsByProviderAsync(provider, cancellationToken);
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
            var insertedOrUpdatedCount = await _unitOfWork.WeatherStations.UpsertRangeAsync(batch, cancellationToken);

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

    public async Task<int> SetAllStationsWithDataToActiveByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        ValidateProvider(provider);
        return await _unitOfWork.WeatherStations.SetAllStationsWithDataToActiveByProviderAsync(provider, cancellationToken);
    }

    public async Task<int> SetAllStationsWithoutDataToInactiveByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        ValidateProvider(provider);
        return await _unitOfWork.WeatherStations.SetAllStationsWithoutDataToInactiveByProviderAsync(provider, cancellationToken);
    }

    public async Task<int> SetStationsActiveByProviderAsync(string provider, IEnumerable<string> stationIds, CancellationToken cancellationToken = default)
    {
        ValidateProvider(provider);
        var stationIdList = NormalizeStationIds(stationIds);
        if (stationIdList.Count == 0)
        {
            return 0;
        }

        var totalUpdated = 0;
        for (int i = 0; i < stationIdList.Count; i += BatchSize)
        {
            var batch = stationIdList.Skip(i).Take(BatchSize).ToList();
            totalUpdated += await _unitOfWork.WeatherStations.SetStationsActiveByProviderAsync(provider, batch, cancellationToken);
        }

        return totalUpdated;
    }

    public async Task<int> SetStationsInactiveByProviderAsync(string provider, IEnumerable<string> stationIds, CancellationToken cancellationToken = default)
    {
        ValidateProvider(provider);
        var stationIdList = NormalizeStationIds(stationIds);
        if (stationIdList.Count == 0)
        {
            return 0;
        }

        var totalUpdated = 0;
        for (int i = 0; i < stationIdList.Count; i += BatchSize)
        {
            var batch = stationIdList.Skip(i).Take(BatchSize).ToList();
            totalUpdated += await _unitOfWork.WeatherStations.SetStationsInactiveByProviderAsync(provider, batch, cancellationToken);
        }

        return totalUpdated;
    }

    public async Task<int> SetMissingStationsInactiveByProviderAsync(string provider, IEnumerable<string> seenStationIds, CancellationToken cancellationToken = default)
    {
        ValidateProvider(provider);
        var stationIdList = NormalizeStationIds(seenStationIds);
        return await _unitOfWork.WeatherStations.SetMissingStationsInactiveByProviderAsync(provider, stationIdList, cancellationToken);
    }

    public async Task<List<WeatherStation>> GetStationsWithMissingCountryAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.WeatherStations.GetStationsWithMissingCountryAsync(cancellationToken);
    }

    public async Task<int> UpdateCountriesAsync(WeatherStation[] weatherStations, CancellationToken cancellationToken = default)
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
            totalAffected += await UpdateCountriesBatchAsync(batch, cancellationToken);
        }

        return totalAffected;
    }

    private async Task<int> UpdateCountriesBatchAsync(List<WeatherStation> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return 0;

        // Use explicit transaction for Supabase connection pooler compatibility
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var updatedCount = await _unitOfWork.WeatherStations.UpdateCountriesAsync(batch, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            return updatedCount;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            _logger.LogError(ex, "Failed to update countries for weather stations batch of {Count} records", batch.Count);
            throw;
        }
    }

    private static void ValidateProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Provider cannot be null or empty", nameof(provider));
        }
    }

    private static List<string> NormalizeStationIds(IEnumerable<string> stationIds)
    {
        if (stationIds == null)
        {
            return [];
        }

        return stationIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(ProviderComparer)
            .ToList();
    }

}


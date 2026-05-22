using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FlexLabs.EntityFrameworkCore.Upsert;
using WindLordApi.Data.Models;

namespace WindLordApi.Data.Repositories;

/// <summary>
/// Repository implementation for ForecastCache entity.
/// </summary>
public class ForecastCacheRepository : Repository<ForecastCache>, IForecastCacheRepository
{
    private readonly ILogger<ForecastCacheRepository> _logger;

    public ForecastCacheRepository(ApplicationDbContext context, ILogger<ForecastCacheRepository> logger)
        : base(context)
    {
        _logger = logger;
    }

    public async Task<int> UpsertRangeAsync(IEnumerable<ForecastCache> entities, CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();
        if (entitiesList.Count == 0) return 0;

        // Use FlexLabs upsert: ON CONFLICT (location_id, time) DO UPDATE
        return await _context.UpsertRange<ForecastCache>(entitiesList)
            .On(fc => new { fc.LocationId, fc.Time })
            .UpdateIf((existing, incoming) => incoming.IsYrData || existing.IsYrData == false)
            .WhenMatched((existing, incoming) => new ForecastCache
            {
                Temperature = incoming.Temperature,
                WindSpeed = incoming.WindSpeed,
                WindDirection = incoming.WindDirection,
                WindGusts = incoming.WindGusts,
                Precipitation = incoming.Precipitation,
                PrecipitationProbability = incoming.PrecipitationProbability,
                PressureMsl = incoming.PressureMsl,
                WeatherCode = incoming.WeatherCode,
                IsDay = incoming.IsDay,
                LandingWind = incoming.LandingWind,
                LandingGust = incoming.LandingGust,
                LandingWindDirection = incoming.LandingWindDirection,
                WindSpeed1000hpa = incoming.WindSpeed1000hpa,
                WindDirection1000hpa = incoming.WindDirection1000hpa,
                WindSpeed925hpa = incoming.WindSpeed925hpa,
                WindDirection925hpa = incoming.WindDirection925hpa,
                WindSpeed850hpa = incoming.WindSpeed850hpa,
                WindDirection850hpa = incoming.WindDirection850hpa,
                WindSpeed700hpa = incoming.WindSpeed700hpa,
                WindDirection700hpa = incoming.WindDirection700hpa,
                Temperature1000hpa = incoming.Temperature1000hpa,
                Temperature925hpa = incoming.Temperature925hpa,
                Temperature850hpa = incoming.Temperature850hpa,
                Temperature700hpa = incoming.Temperature700hpa,
                CloudCover = incoming.CloudCover,
                CloudCoverLow = incoming.CloudCoverLow,
                CloudCoverMid = incoming.CloudCoverMid,
                CloudCoverHigh = incoming.CloudCoverHigh,
                Cape = incoming.Cape,
                ConvectiveInhibition = incoming.ConvectiveInhibition,
                LiftedIndex = incoming.LiftedIndex,
                BoundaryLayerHeight = incoming.BoundaryLayerHeight,
                FreezingLevelHeight = incoming.FreezingLevelHeight,
                GeopotentialHeight1000hpa = incoming.GeopotentialHeight1000hpa,
                GeopotentialHeight925hpa = incoming.GeopotentialHeight925hpa,
                GeopotentialHeight850hpa = incoming.GeopotentialHeight850hpa,
                GeopotentialHeight700hpa = incoming.GeopotentialHeight700hpa,
                UpdatedAt = incoming.UpdatedAt,
                PrecipitationMax = incoming.PrecipitationMax,
                PrecipitationMin = incoming.PrecipitationMin,
                IsYrData = incoming.IsYrData
            })
            .RunAsync(cancellationToken);
    }

    public async Task<int> DeleteOldForecastsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default)
    {
        var deleted = await _dbSet
            .Where(fc => fc.Time < cutoffTime)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogDebug("Deleted {Count} forecasts older than {CutoffTime}", deleted, cutoffTime);
        return deleted;
    }
}


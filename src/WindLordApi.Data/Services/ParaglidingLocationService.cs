using Microsoft.Extensions.Logging;
using WindLordApi.Data.Models;
using WindLordApi.Data.Repositories;

namespace WindLordApi.Data.Services;

/// <summary>
/// Service implementation for ParaglidingLocation entity operations.
/// </summary>
public class ParaglidingLocationService : IParaglidingLocationService
{
    private readonly IUnitOfWork _unitOfWork;
    public ParaglidingLocationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ParaglidingLocation>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ParaglidingLocations.GetByIdsAsync(ids, cancellationToken);
    }

    public async Task<IEnumerable<LocationsWithOldestForecast>> GetLocationsWithOldestForecastAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ParaglidingLocations.GetLocationsWithOldestForecastAsync(limit, cancellationToken);
    }

    public async Task<IEnumerable<LocationsWithoutForecast>> GetLocationsWithoutForecastAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ParaglidingLocations.GetLocationsWithoutForecastAsync(limit, cancellationToken);
    }

    public async Task<IEnumerable<Guid>> GetOpenMeteoRefreshCandidatesAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ParaglidingLocations.GetOpenMeteoRefreshCandidatesAsync(limit, cancellationToken);
    }
}


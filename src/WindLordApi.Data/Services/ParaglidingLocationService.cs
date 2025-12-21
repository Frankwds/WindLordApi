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
    private readonly ILogger<ParaglidingLocationService> _logger;
    private const int BatchSize = 1000; // Process in batches to avoid parameter limits

    public ParaglidingLocationService(
        IUnitOfWork unitOfWork,
        ILogger<ParaglidingLocationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
}


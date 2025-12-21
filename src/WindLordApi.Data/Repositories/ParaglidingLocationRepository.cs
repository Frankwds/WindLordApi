using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FlexLabs.EntityFrameworkCore.Upsert;
using WindLordApi.Data.Models;

namespace WindLordApi.Data.Repositories;

/// <summary>
/// Repository implementation for ParaglidingLocation entity.
/// </summary>
public class ParaglidingLocationRepository : Repository<ParaglidingLocation>, IParaglidingLocationRepository
{
    private readonly ILogger<ParaglidingLocationRepository> _logger;

    public ParaglidingLocationRepository(ApplicationDbContext context, ILogger<ParaglidingLocationRepository> logger)
        : base(context)
    {
        _logger = logger;
    }

}

